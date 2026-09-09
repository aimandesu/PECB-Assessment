/**
 * All HTTP traffic goes through the `/api` prefix, which the Angular dev server
 * proxies to the ASP.NET Core host (see `proxy.conf.json`).
 *
 * Using a proxy instead of the absolute backend URL means the browser never makes
 * a cross-origin request, so the backend does not need a CORS policy for local dev.
 */
export const environment = {
  production: false,
  apiBaseUrl: '/api',
};
