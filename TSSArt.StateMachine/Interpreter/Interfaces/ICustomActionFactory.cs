using System.Xml;

namespace TSSArt.StateMachine
{
	public interface ICustomActionFactory
	{
		void FillXmlNameTable(XmlNameTable xmlNameTable);

		bool CanHandle(string ns, string name);

		ICustomActionExecutor CreateExecutor(string xml);
	}
}