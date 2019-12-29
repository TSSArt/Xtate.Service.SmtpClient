using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#Input", Alias = "input")]
	public class InputService : SimpleServiceBase
	{
        public static readonly IServiceFactory Factory = SimpleServiceFactory<InputService>.Instance;
		private DataModelArray _controls;

		protected override ValueTask<DataModelValue> Execute()
        {
			var parameters = Content.AsObjectOrEmpty();
			_controls = parameters["controls"].AsArrayOrEmpty();

			var task = Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			return new ValueTask<DataModelValue>(task);
        }

		private DataModelValue Show()
		{
			using var form = new InputForm();

			foreach (var control in _controls)
			{
				var fieldObj = control.AsObjectOrEmpty();
				var name = fieldObj["name"].AsStringOrDefault();
				var location = fieldObj["location"].AsStringOrDefault();
				var type = fieldObj["type"].AsStringOrDefault();
				
				form.AddInput(name, location, type);
			}

			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, default));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			var result = new DataModelObject();

			if (form.DialogResult == DialogResult.OK)
			{
				result["status"] = new DataModelValue("ok");

				var parameters = new DataModelObject();
				foreach (var pair in form.Result)
				{
					parameters[pair.Key] = new DataModelValue(pair.Value);
				}
				
				parameters.Freeze();
				
				result["parameters"] = new DataModelValue(parameters);
				result.Freeze();
			}
			else
			{
				result["status"] = new DataModelValue("cancel");
			}

			result.Freeze();

			return new DataModelValue(result);
		}
	}
}