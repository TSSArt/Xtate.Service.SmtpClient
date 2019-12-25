using System;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#Input", Alias = "input")]
	public class InputService : SimpleServiceBase
	{
        public static readonly IServiceFactory Factory = SimpleServiceFactory<InputService>.Instance;

        protected override ValueTask<DataModelValue> Execute()
        {
            throw  new NotImplementedException();
        }
    }
}