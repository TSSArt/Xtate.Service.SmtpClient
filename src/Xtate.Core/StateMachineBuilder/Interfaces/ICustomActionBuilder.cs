namespace Xtate
{
	public interface ICustomActionBuilder
	{
		ICustomAction Build();

		void SetXml(string xml);
	}
}