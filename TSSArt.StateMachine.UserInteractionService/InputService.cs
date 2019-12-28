using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#Input", Alias = "input")]
	public class InputService : SimpleServiceBase
	{
        public static readonly IServiceFactory Factory = SimpleServiceFactory<InputService>.Instance;
		private DataModelArray _fields;

		protected override ValueTask<ServiceResult> Execute()
        {
			var parameters = Content.AsObjectOrEmpty();
			_fields = parameters["fields"].AsArrayOrEmpty();

			var task = Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			return new ValueTask<ServiceResult>(task);
        }

		private ServiceResult Show()
		{
			using var form = new InputForm();

			foreach (var field in _fields)
			{
				var fieldObj = field.AsObjectOrEmpty();
				var name = fieldObj["name"].AsStringOrDefault();
				var location = fieldObj["location"].AsStringOrDefault();
				var type = fieldObj["type"].AsStringOrDefault();
				
				form.AddInput(name, location, type);
			}

			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, default));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			if (form.DialogResult == DialogResult.OK)
			{
				var result = new DataModelObject();

				foreach (var pair in form.Result)
				{
					result[pair.Key] = new DataModelValue(pair.Value);
				}

				result.Freeze();

				return new ServiceResult { DoneEventData = new DataModelValue(result), DoneEventSuffix = "ok" };
			}

			return new ServiceResult { DoneEventSuffix = "cancel" };

		}
	}
}