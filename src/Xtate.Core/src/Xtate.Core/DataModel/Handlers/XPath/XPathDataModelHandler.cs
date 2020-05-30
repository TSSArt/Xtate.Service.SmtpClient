namespace Xtate.DataModel.XPath
{
	internal sealed class XPathDataModelHandler : DataModelHandlerBase
	{
		public const string DataModelType = "xpath";

		public static readonly IDataModelHandlerFactory Factory = new DataModelHandlerFactory();

		private XPathDataModelHandler(IErrorProcessor errorProcessor) : base(errorProcessor) { }

		private class DataModelHandlerFactory : IDataModelHandlerFactory
		{
		#region Interface IDataModelHandlerFactory

			public bool CanHandle(string dataModelType) => dataModelType == DataModelType;

			public IDataModelHandler CreateHandler(IErrorProcessor errorProcessor) => new XPathDataModelHandler(errorProcessor);

		#endregion
		}
	}
}