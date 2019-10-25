namespace TSSArt.StateMachine
{
	public class StateMachineFluentBuilder
	{
		private readonly IStateMachineBuilder _builder;
		private readonly IBuilderFactory      _factory;

		public StateMachineFluentBuilder(IBuilderFactory factory)
		{
			_factory = factory;
			_builder = factory.CreateStateMachineBuilder();
			_builder.SetDataModelType("runtime");
		}

		public IStateMachine Build() => _builder.Build();

		public StateMachineFluentBuilder SetInitial(params Identifier[] initial)
		{
			_builder.SetInitial(IdentifierList.Create(initial));
			return this;
		}

		public StateFluentBuilder<StateMachineFluentBuilder> BeginState() => new StateFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddState);

		public ParallelFluentBuilder<StateMachineFluentBuilder> BeginParallel() => new ParallelFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddParallel);

		public FinalFluentBuilder<StateMachineFluentBuilder> BeginFinal() => new FinalFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddFinal);

		public StateFluentBuilder<StateMachineFluentBuilder> BeginState(Identifier id) => new StateFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddState).SetId(id);

		public ParallelFluentBuilder<StateMachineFluentBuilder> BeginParallel(Identifier id) => new ParallelFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddParallel).SetId(id);

		public FinalFluentBuilder<StateMachineFluentBuilder> BeginFinal(Identifier id) => new FinalFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddFinal).SetId(id);
	}
}