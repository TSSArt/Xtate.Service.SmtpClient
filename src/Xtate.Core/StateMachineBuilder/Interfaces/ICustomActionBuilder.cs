namespace TSSArt.StateMachine
{
	public interface ICustomActionBuilder
	{
		ICustomAction Build();

		void SetXml(string xml);
	}
}