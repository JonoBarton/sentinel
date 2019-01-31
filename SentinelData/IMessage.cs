using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SentinelData
{
    [ServiceContract]
    public interface IMessage
    {
        [OperationContract]
        string Message(string value);
    }
}
