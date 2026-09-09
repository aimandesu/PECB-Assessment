using Microsoft.EntityFrameworkCore;
using PECB_BE.Database;
using PECB_BE.Entities;
using PECB_BE.Enums;

namespace PECB_BE.Extension;

/// <summary>
/// Development-only startup work, so a fresh clone runs without any manual setup.
/// </summary>
public static class DevelopmentSetupExtension
{
    /// <summary>
    /// Creates or migrates the database and seeds a demo data set.
    /// </summary>
    /// <remarks>
    /// This only runs in the Development environment. Applying migrations at startup is
    /// deliberate here so a reviewer does not need the dotnet-ef global tool; a production
    /// deployment should run `dotnet ef database update` as its own step instead.
    /// </remarks>
    public static async Task PrepareDevelopmentDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DevelopmentSetupExtension));

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            // A missing or unreachable database should not stop the host from starting:
            // the failure is far easier to diagnose from a logged message than from a
            // crash at boot before any request is served.
            logger.LogError(
                exception,
                "Could not prepare the development database. Check ConnectionStrings:DefaultConnection.");
            return;
        }

        // Seed only into a genuinely empty database, so restarting the app never duplicates
        // rows and never touches data someone has already entered.
        if (await context.Agents.AnyAsync() || await context.Tickets.AnyAsync())
        {
            return;
        }

        var agents = SeedAgents(context);
        var overdueCount = SeedTickets(context, agents);

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded {Agents} agents and {Tickets} tickets ({Overdue} overdue) for development.",
            agents.Count,
            TicketSeeds.Length,
            overdueCount);
    }

    private static List<Agent> SeedAgents(ApplicationDbContext context)
    {
        var agents = AgentSeeds
            .Select(seed => new Agent
            {
                FullName = seed.FullName,
                Department = seed.Department,
                Active = seed.Active,
            })
            .ToList();

        context.Agents.AddRange(agents);
        return agents;
    }

    /// <returns>How many of the seeded tickets are past their due date.</returns>
    private static int SeedTickets(ApplicationDbContext context, List<Agent> agents)
    {
        var now = DateTimeOffset.UtcNow;
        var overdue = 0;

        foreach (var seed in TicketSeeds)
        {
            var createdDate = now.AddHours(-seed.CreatedHoursAgo);
            var dueDate = createdDate + SlaFor(seed.Priority);

            var ticket = new Ticket
            {
                Title = seed.Title,
                Description = seed.Description,
                CustomerName = seed.CustomerName,
                CustomerEmail = seed.CustomerEmail,
                // Priority is assigned before Status on purpose: its setter recalculates the
                // due date and bails out early once the status is Resolved or Closed.
                Priority = seed.Priority,
                Status = seed.Status,
                ResolvedDate = seed.ResolvedHoursAgo is { } resolved ? now.AddHours(-resolved) : null,
                ClosedDate = seed.ClosedHoursAgo is { } closed ? now.AddHours(-closed) : null,
                LastModifiedDate = now.AddHours(-seed.CreatedHoursAgo / 2),
            };

            if (seed.Agent >= 0)
            {
                ticket.AssignAgent(agents[seed.Agent]);
            }

            context.Tickets.Add(ticket);

            // CreatedDate is private and DueDate has a private setter, so the entity cannot
            // be backdated through its own API — and without backdating, nothing could ever
            // be overdue. EF's change tracker can write both, which keeps the demo data
            // realistic without loosening the entity's encapsulation.
            var entry = context.Entry(ticket);
            entry.Property("CreatedDate").CurrentValue = createdDate;
            entry.Property("DueDate").CurrentValue = dueDate;

            foreach (var (body, index) in seed.Comments.Select((body, index) => (body, index)))
            {
                // Space the thread out between the ticket being raised and now.
                var offset = seed.CreatedHoursAgo * (index + 1) / (seed.Comments.Length + 1);

                context.Comments.Add(new Comment
                {
                    TicketId = ticket.Id,
                    AuthorName = seed.Agent >= 0 ? agents[seed.Agent].FullName : seed.CustomerName,
                    Body = body,
                    CreatedDate = now.AddHours(-(seed.CreatedHoursAgo - offset)),
                });
            }

            if (dueDate < now && seed.Status is not (Status.Resolved or Status.Closed))
            {
                overdue++;
            }
        }

        return overdue;
    }

    /// <summary>Mirrors the SLA in <c>Ticket.RecalculateDueDate</c>.</summary>
    private static TimeSpan SlaFor(Priority priority) => priority switch
    {
        Priority.Critical => TimeSpan.FromHours(4),
        Priority.High => TimeSpan.FromDays(1),
        Priority.Normal => TimeSpan.FromDays(3),
        Priority.Low => TimeSpan.FromDays(7),
        _ => TimeSpan.FromDays(3),
    };

    private sealed record AgentSeed(string FullName, Department Department, bool Active = true);

    private sealed record TicketSeed(
        string Title,
        string Description,
        string CustomerName,
        string CustomerEmail,
        Priority Priority,
        Status Status,
        // Index into the seeded roster; -1 leaves the ticket unassigned.
        int Agent,
        double CreatedHoursAgo,
        double? ResolvedHoursAgo = null,
        double? ClosedHoursAgo = null,
        string[]? CommentBodies = null)
    {
        public string[] Comments => CommentBodies ?? [];
    }

    private static readonly AgentSeed[] AgentSeeds =
    [
        new("Dana Reyes", Department.Technical),
        new("Sam Okafor", Department.Billing),
        new("Priya Nair", Department.Technical),
        new("Marcus Lindqvist", Department.General),
        // Inactive on purpose: the API refuses to assign an inactive agent, and the UI
        // leaves them out of the assignment dropdown.
        new("Nina Ferrer", Department.Billing, Active: false),
    ];

    /// <summary>
    /// Twenty tickets spread across every status and priority. Five are deliberately past
    /// their due date so the overdue highlighting and the "Overdue only" filter have
    /// something to show.
    /// </summary>
    private static readonly TicketSeed[] TicketSeeds =
    [
        // ---- Overdue: due date passed while still open ----
        new("Cannot log in to the certification portal",
            "SSO returns 'account not found' for every user in our tenant since this morning.",
            "Northwind Group", "it-ops@northwind.test",
            Priority.Critical, Status.InProgress, Agent: 0, CreatedHoursAgo: 9,
            CommentBodies:
            [
                "Reproduced on our side. Escalating to the identity team.",
                "Identity team confirmed a stale certificate. Rotation in progress.",
            ]),

        new("Payment failed but the card was charged",
            "Checkout reported a failure yet the amount was debited. Order reference INV-88213.",
            "Bluewave Consulting", "finance@bluewave.test",
            Priority.Critical, Status.New, Agent: -1, CreatedHoursAgo: 6),

        new("Exam scheduling page returns a 500 error",
            "Selecting any date in November throws a server error before the summary loads.",
            "Halden Institute", "admin@halden.test",
            Priority.High, Status.InProgress, Agent: 1, CreatedHoursAgo: 40,
            CommentBodies: ["Narrowed it down to the timezone conversion on the booking step."]),

        new("Certificate PDF downloads are corrupted",
            "Downloaded certificates will not open. The file size looks far too small.",
            "Orion Safety Ltd", "quality@orionsafety.test",
            Priority.Normal, Status.InProgress, Agent: 2, CreatedHoursAgo: 100,
            CommentBodies:
            [
                "Confirmed the template renders blank for the ISO 27001 course.",
                "Waiting on the vendor for an updated rendering library.",
            ]),

        new("Bulk invoice export is missing rows",
            "The quarterly export returns 480 of 512 invoices. The gap is not consistent.",
            "Kestrel Logistics", "accounts@kestrel.test",
            Priority.Low, Status.New, Agent: 3, CreatedHoursAgo: 200),

        // ---- Open and still within their SLA ----
        new("Password reset email never arrives",
            "Reset requests are not delivered, including to addresses on our allow list.",
            "Vertex Analytics", "support@vertex.test",
            Priority.High, Status.New, Agent: -1, CreatedHoursAgo: 2),

        new("Exam results not showing in the dashboard",
            "Candidates completed the exam yesterday but the dashboard still shows pending.",
            "Meridian Training", "ops@meridian.test",
            Priority.Critical, Status.InProgress, Agent: 0, CreatedHoursAgo: 1,
            CommentBodies: ["Checking whether the results job ran overnight."]),

        new("Request a refund for a duplicate order",
            "We were billed twice for the same seat. Please refund the second charge.",
            "Aldridge & Co", "billing@aldridge.test",
            Priority.High, Status.InProgress, Agent: 1, CreatedHoursAgo: 10),

        new("Add a second administrator to our account",
            "We need a backup admin who can manage bookings while the primary is on leave.",
            "Solstice Energy", "hr@solstice.test",
            Priority.Normal, Status.InProgress, Agent: 2, CreatedHoursAgo: 20),

        new("Training material link is broken",
            "The module 3 workbook link on the course page returns a 404.",
            "Cobalt Manufacturing", "training@cobalt.test",
            Priority.Normal, Status.New, Agent: -1, CreatedHoursAgo: 5),

        new("Update the billing address on our account",
            "We have moved offices and the new address needs to appear on future invoices.",
            "Fairmont Retail", "finance@fairmont.test",
            Priority.Low, Status.New, Agent: -1, CreatedHoursAgo: 12),

        new("Change the company name on a certificate",
            "Our legal entity was renamed after the certificate was issued. Reissue needed.",
            "Lumen Group", "legal@lumen.test",
            Priority.Low, Status.InProgress, Agent: 3, CreatedHoursAgo: 30,
            CommentBodies: ["Reissue requires proof of the name change. Requested from the customer."]),

        // ---- Resolved, awaiting closure ----
        new("Cannot upload proof of payment",
            "The upload dialog rejects PDFs over 2 MB without showing an error message.",
            "Ridgeline Partners", "admin@ridgeline.test",
            Priority.High, Status.Resolved, Agent: 1, CreatedHoursAgo: 72, ResolvedHoursAgo: 60,
            CommentBodies:
            [
                "The limit was set to 2 MB by mistake. Raised it to 20 MB.",
                "Customer confirmed the upload now works.",
            ]),

        new("Invoice shows the wrong VAT rate",
            "Invoice INV-90114 applies 21 percent. Our registered rate is 19 percent.",
            "Brandt Systems", "ap@brandt.test",
            Priority.Normal, Status.Resolved, Agent: 3, CreatedHoursAgo: 96, ResolvedHoursAgo: 80,
            CommentBodies: ["Corrected the tax profile and issued a credit note."]),

        new("Course access expired earlier than expected",
            "Access ended after 30 days although our contract covers 90 days.",
            "Ashford Marine", "learning@ashford.test",
            Priority.Critical, Status.Resolved, Agent: 0, CreatedHoursAgo: 50, ResolvedHoursAgo: 47,
            CommentBodies: ["Extended the enrolment to the contracted end date."]),

        new("Newsletter unsubscribe link does nothing",
            "Clicking unsubscribe loads a blank page and emails keep arriving.",
            "Pinehurst Academy", "info@pinehurst.test",
            Priority.Low, Status.Resolved, Agent: 2, CreatedHoursAgo: 200, ResolvedHoursAgo: 100),

        // ---- Closed: read-only in the UI ----
        new("Duplicate account created by mistake",
            "Two accounts exist for the same company. Please merge them into the older one.",
            "Talbot Chemicals", "it@talbot.test",
            Priority.Normal, Status.Closed, Agent: 2,
            CreatedHoursAgo: 240, ResolvedHoursAgo: 200, ClosedHoursAgo: 190,
            CommentBodies:
            [
                "Merged the newer account and moved its two bookings across.",
                "Customer confirmed. Closing this out.",
            ]),

        new("Question about the group discount",
            "How many seats do we need to book before the group rate applies?",
            "Everly Foods", "purchasing@everly.test",
            Priority.Low, Status.Closed, Agent: 3,
            CreatedHoursAgo: 300, ResolvedHoursAgo: 260, ClosedHoursAgo: 250),

        new("Login loop after changing password",
            "After a password change the portal redirects back to the login page endlessly.",
            "Sterling Rail", "helpdesk@sterlingrail.test",
            Priority.High, Status.Closed, Agent: 0,
            CreatedHoursAgo: 150, ResolvedHoursAgo: 140, ClosedHoursAgo: 130,
            CommentBodies: ["Stale session cookie. Cleared server side and advised a hard refresh."]),

        new("Wrong exam language was selected",
            "The candidate booked the French paper but needs the English one instead.",
            "Norbridge Utilities", "exams@norbridge.test",
            Priority.Critical, Status.Closed, Agent: 1,
            CreatedHoursAgo: 120, ResolvedHoursAgo: 118, ClosedHoursAgo: 110,
            CommentBodies: ["Rebooked onto the English paper at no extra cost."]),
    ];
}
