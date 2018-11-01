using Carta.External.Logic.Objects;
using Carta.External.Logic.Processor;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;

namespace Carta.External.API
{

    public class Service : IService
    {
        private readonly static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Stream GetData(Stream streamRequest)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (streamRequest == null)
                throw new WebFaultException(HttpStatusCode.BadRequest);

            string GUID = Guid.NewGuid().ToString("N");

            using (ThreadContext.Stacks["NDC"].Push(GUID))
            {
                StreamReader sReader = new StreamReader(streamRequest);
                StringBuilder sbRequest = new StringBuilder(sReader.ReadToEnd());

                log.Info(string.Format("GTW API REQUEST: {0}", sbRequest.ToString()));

                GtwApiProcessor gtwApiProcessor = new GtwApiProcessor(sbRequest.ToString());

                string response = gtwApiProcessor.ProcessPostRequest();
                if(string.IsNullOrEmpty(response))
                    throw new WebFaultException(HttpStatusCode.BadRequest);

                stopwatch.Stop();
                log.Info("REQUEST TIME DIFFERENCE : " + stopwatch.ElapsedMilliseconds);
                return GetResponse(response);

            }

        }

        public Stream GetResponse(string response)
        {
            byte[] resultByte = Encoding.UTF8.GetBytes(response);
            return new MemoryStream(resultByte);
        }

    }

    public class RawWebContentTypeMapper : WebContentTypeMapper
    {
        /// <summary>
        /// Get message format for content type
        /// </summary>
        /// <param name="contentType">The content type</param>
        /// <returns>WebContentFormat</returns>
        public override WebContentFormat GetMessageFormatForContentType(string contentType)
        {
            return WebContentFormat.Raw;
        }
    }
}
