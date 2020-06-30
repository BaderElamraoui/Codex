using Carta.Api.External.Dal.Db;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Carta.Api.External.Logic.Http
{
    public class HttpManager
    {

        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        HttpWebResponse httpResponse = null;

        public bool TryCall(string request, List<Header> headers, string endpoint, string httpMethod, out string response, out HttpStatusCode statusCode)
        {

            response = string.Empty;
            statusCode = HttpStatusCode.BadRequest;
            log.Info("Start HTTP Call to: " + endpoint);
            log.Debug(string.Format("REQUEST: {0}", request));
            if (string.IsNullOrEmpty(request) || string.IsNullOrEmpty(endpoint))
                return false;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);

                if (httpMethod == "POST")
                {
                    httpWebRequest.Method = WebRequestMethods.Http.Post;
                }
                else if (httpMethod == "PUT")
                {
                    httpWebRequest.Method = WebRequestMethods.Http.Put;
                }
                else
                    httpWebRequest.Method = WebRequestMethods.Http.Get;

                log.Info("Adding headers to HTTP Request");
                if (headers != null && headers.Any())
                {
                    headers.ForEach(x =>
                    {

                        log.InfoFormat("Header ID to add: {0}", x.id);
                        if (x.id == "Content-Type")
                            httpWebRequest.ContentType = x.value;
                        else
                            httpWebRequest.Headers[x.id] = x.value;

                    });
                }

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(request);
                }

                using (httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    log.InfoFormat("Server response: {0}", httpResponse.StatusCode);
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            response = streamReader.ReadToEnd();
                            statusCode = HttpStatusCode.OK;
                        }
                    }
                }

            }
            catch (WebException ex)
            {
                httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    response = streamReader.ReadToEnd();
                    statusCode = httpResponse.StatusCode;
                }
                log.ErrorFormat("Error During HTTP status Code Call: {0}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error During HTTP Call: {0}", ex.Message);
                statusCode = HttpStatusCode.BadRequest;
                return false;
            }

            log.DebugFormat("RESPONSE: {0}", response);
            log.Info("End HTTP Call");
            return true;

        }

        public bool TryCallGetClientId(string request, WebHeaderCollection headers, string endpoint, string httpMethod, out string response, out HttpStatusCode statusCode)
        {

            response = string.Empty;
            statusCode = HttpStatusCode.BadRequest;
            log.Info("Start HTTP Call to: " + endpoint);
            log.Debug(string.Format("REQUEST: {0}", request));
            if (string.IsNullOrEmpty(request) || string.IsNullOrEmpty(endpoint))
                return false;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);

                 httpWebRequest.Method = WebRequestMethods.Http.Post;

                log.Info("Adding headers to HTTP Request");
                if (headers != null)
                {
                    httpWebRequest.Headers[HttpRequestHeader.Authorization] = headers[HttpRequestHeader.Authorization];
                    httpWebRequest.ContentType = headers[HttpRequestHeader.ContentType];
                }

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(request);
                }

                using (httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    log.InfoFormat("Server response: {0}", httpResponse.StatusCode);
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            response = streamReader.ReadToEnd();
                            statusCode = HttpStatusCode.OK;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                httpResponse = (HttpWebResponse)ex.Response;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    response = streamReader.ReadToEnd();
                    statusCode = httpResponse.StatusCode;
                }
                log.ErrorFormat("Error During HTTP status Code Call: {0}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error During HTTP Call: {0}", ex.Message);
                statusCode = HttpStatusCode.BadRequest;
                return false;
            }

            log.DebugFormat("RESPONSE: {0}", response);
            log.Info("End HTTP Call");
            return true;

        }

    }


}
