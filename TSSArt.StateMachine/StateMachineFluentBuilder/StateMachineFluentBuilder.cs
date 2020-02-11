using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class StateMachineFluentBuilder
	{
		private readonly IStateMachineBuilder _builder;
		private readonly IBuilderFactory      _factory;

		public StateMachineFluentBuilder(IBuilderFactory factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_builder = factory.CreateStateMachineBuilder();
			_builder.SetDataModelType("runtime");
		}

		public IStateMachine Build() => _builder.Build();

		public StateMachineFluentBuilder SetInitial(params string[] initial)
		{
			if (initial == null) throw new ArgumentNullException(nameof(initial));
			if (initial.Length == 0) throw new ArgumentException(message: "Value cannot be an empty collection.", nameof(initial));

			var builder = ImmutableArray.CreateBuilder<IIdentifier>(initial.Length);

			foreach (var s in initial)
			{
				builder.Add((Identifier) s);
			}

			_builder.SetInitial(builder.MoveToImmutable());

			return this;
		}

		public StateMachineFluentBuilder SetInitial(params IIdentifier[] initial)
		{
			if (initial == null) throw new ArgumentNullException(nameof(initial));
			if (initial.Length == 0) throw new ArgumentException(message: "Value cannot be an empty collection.", nameof(initial));

			_builder.SetInitial(initial.ToImmutableArray());

			return this;
		}

		public StateMachineFluentBuilder SetInitial(ImmutableArray<string> initial)
		{
			if (initial.IsDefaultOrEmpty) throw new ArgumentException(message: "Value cannot be an empty list.", nameof(initial));

			_builder.SetInitial(ImmutableArray.CreateRange<string, IIdentifier>(initial, id => (Identifier) id));

			return this;
		}

		public StateMachineFluentBuilder SetInitial(ImmutableArray<IIdentifier> initial)
		{
			if (initial.IsDefaultOrEmpty) throw new ArgumentException(message: "Value cannot be an empty list.", nameof(initial));

			_builder.SetInitial(initial);

			return this;
		}

		public StateFluentBuilder<StateMachineFluentBuilder> BeginState() => new StateFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddState);

		public ParallelFluentBuilder<StateMachineFluentBuilder> BeginParallel() => new ParallelFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddParallel);

		public FinalFluentBuilder<StateMachineFluentBuilder> BeginFinal() => new FinalFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddFinal);

		public StateFluentBuilder<StateMachineFluentBuilder> BeginState(string id) => BeginState((Identifier) id);

		public StateFluentBuilder<StateMachineFluentBuilder> BeginState(IIdentifier id) => new StateFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddState).SetId(id);

		public ParallelFluentBuilder<StateMachineFluentBuilder> BeginParallel(string id) => BeginParallel((Identifier) id);

		public ParallelFluentBuilder<StateMachineFluentBuilder> BeginParallel(IIdentifier id) => new ParallelFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddParallel).SetId(id);

		public FinalFluentBuilder<StateMachineFluentBuilder> BeginFinal(string id) => BeginFinal((Identifier) id);

		public FinalFluentBuilder<StateMachineFluentBuilder> BeginFinal(IIdentifier id) => new FinalFluentBuilder<StateMachineFluentBuilder>(_factory, this, _builder.AddFinal).SetId(id);
	}
}