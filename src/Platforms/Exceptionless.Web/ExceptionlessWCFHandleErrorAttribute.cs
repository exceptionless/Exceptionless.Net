using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Web;
using Exceptionless.Plugins;

namespace Exceptionless.Web {
    // TODO: Research this more: http://www.olegsych.com/2008/07/simplifying-wcf-using-exceptions-as-faults/
    [AttributeUsage(AttributeTargets.Class)]
    public class ExceptionlessWcfHandleErrorAttribute : Attribute, IErrorHandler, IServiceBehavior {
        public virtual void ProvideFault(Exception error, MessageVersion version, ref Message fault) {
            var faultException = new FaultException(error.Message);
            MessageFault messageFault = faultException.CreateMessageFault();
            fault = Message.CreateMessage(version, messageFault, "Error");
        }

        public virtual bool HandleError(Exception exception) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("WCFServiceError");

            if (HttpContext.Current != null)
                contextData.Add("HttpContext", HttpContext.Current.ToWrapped());

            exception.ToExceptionless(contextData).Submit();

            return true;
        }

        public virtual void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {}

        public virtual void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) {}

        public virtual void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
            foreach (ChannelDispatcher chanDisp in serviceHostBase.ChannelDispatchers)
                chanDisp.ErrorHandlers.Add(this);
        }
    }
}