using System.Xml;

namespace Xtate
{
	public interface ICustomActionFactory
	{
		void FillXmlNameTable(XmlNameTable xmlNameTable);

		bool CanHandle(string ns, string name);

		ICustomActionExecutor CreateExecutor(ICustomActionContext customActionContext);
	}
}