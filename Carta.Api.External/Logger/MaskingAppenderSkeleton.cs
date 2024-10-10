using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Carta.Api.External.Logger
{
    public class MaskingAppenderSkeleton : ForwardingAppender
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
                return;

            var renderedMessage = loggingEvent.RenderedMessage;
            var maskedMessage = DataMasker.MaskSensitiveData(renderedMessage);

            // Create a new LoggingEvent with the masked message
            var maskedEvent = new LoggingEvent(
                loggingEvent.GetType(),
                loggingEvent.Repository,
                loggingEvent.LoggerName,
                loggingEvent.Level,
                maskedMessage,
                loggingEvent.ExceptionObject);

            // Forward the masked event
            base.Append(maskedEvent);
        }
    }
}