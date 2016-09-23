using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Exceptionless.SampleWcf {
    [ServiceContract]
    public interface IService1 {
        [OperationContract]
        string GetData(int value);

        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);
    }

    [DataContract]
    public class CompositeType {
        private bool _boolValue = true;
        private string _stringValue = "Hello World";

        [DataMember]
        public bool BoolValue { get { return _boolValue; } set { _boolValue = value; } }

        [DataMember]
        public string StringValue { get { return _stringValue; } set { _stringValue = value; } }
    }
}