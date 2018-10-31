using Carta.External.Dal.Db;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Carta.External.Logic.Http
{
    public class HttpManager
    {

        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Process(string request, List<Header> headers, string endpoint)
        {

            string response = string.Empty;
            log.Info("Start HTTP Call to: " + endpoint);
            log.Debug(string.Format("REQUEST: {0}", request));
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);
                httpWebRequest.Method = "POST";
                log.Info("Adding headers to HTTP Request");
                headers.ForEach(x =>
                {

                    log.Info(string.Format("Header ID to add: {0}", x.id));
                    httpWebRequest.Headers[x.id] = x.value;

                });
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(request);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    response = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Error During HTTP Call: {0}", ex.Message));
            }


            log.Debug(string.Format("RESPONSE: {0}", response));
            log.Info("End HTTP Call");

            return response;

        }

    }


}
