namespace TSSArt.StateMachine
{
	internal interface IDataModelValueProvider
	{
		DataModelValue Arguments        { get; }
		DataModelValue Configuration    { get; }
		DataModelValue Host             { get; }
		DataModelValue Interpreter      { get; }
		DataModelValue DataModelHandler { get; }
	}
}