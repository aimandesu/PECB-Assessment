using Microsoft.EntityFrameworkCore;

namespace PECB_BE.Shared;

public class Paginate<T>
{
    public List<T> Data { get; private set; }
    public int CurrentPage { get; private set; }
    public int PageSize { get; private set; }
    public int Total { get; private set; }
    public int LastPage { get; private set; }

    private Paginate(
        List<T> data, 
        int currentPage, 
        int pageSize,  
        int total,
        int lastPage)
    {
        Data = data;
        CurrentPage = currentPage;
        PageSize = pageSize;
        Total = total;
        LastPage = lastPage;
    }


    public static async Task<Paginate<T>> Pagination(
        IQueryable<T> query,
        int currentPage,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (currentPage < 1) currentPage = 1;

        var total = await query.CountAsync(cancellationToken);
        var skip =  (currentPage - 1) * pageSize;
        
        var result = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        var lastPage = (int)Math.Ceiling(total / (double)pageSize);
        
        return new Paginate<T>(result, currentPage, pageSize,  total, lastPage);
    }

    public Paginate<TNew> TransformTo<TNew>(List<TNew> newData)
    {
        return new Paginate<TNew>(newData, CurrentPage, PageSize, Total, LastPage);
    }

}