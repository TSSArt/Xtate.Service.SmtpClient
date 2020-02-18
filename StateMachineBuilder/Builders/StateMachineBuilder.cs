using System;
using System.Collections.Immutable;
using System.ComponentModel;

namespace TSSArt.StateMachine
{
	public class StateMachineBuilder : IStateMachineBuilder, IStateMachineOptionsBuilder
	{
		private BindingType                          _bindingType;
		private IDataModel                           _dataModel;
		private string                               _dataModelType;
		private ImmutableArray<IIdentifier>          _initialId;
		private string                               _name;
		private IScript                              _script;
		private ImmutableArray<IStateEntity>.Builder _states;

		private PersistenceLevel? _persistenceLevel;
		private bool?             _synchronousEventProcessing;
		private int?              _externalQueueSize;

		public IStateMachine Build()
		{
			var initial = _initialId != null ? (IInitial) new Initial { Transition = new Transition { Target = _initialId } } : null;

			if (initial != null && _states == null)
			{
				throw new InvalidOperationException(message: "Initial state/property cannot be used without any states");
			}

			IStateMachine stateMachine = new StateMachine
										 {
												 Ancestor = new StateMachineOptions
															{
																	PersistenceLevel = _persistenceLevel,
																	ExternalQueueSize = _externalQueueSize, 
																	SynchronousEventProcessing = _synchronousEventProcessing
															},
												 Name = _name, Initial = initial, DataModelType = _dataModelType, Binding = _bindingType,
												 States = _states?.ToImmutable() ?? default, DataModel = _dataModel, Script = _script
										 };

			NoneDataModelHandler.Validate(stateMachine);
			RuntimeDataModelHandler.Validate(stateMachine);

			return stateMachine;
		}

		public void SetInitial(ImmutableArray<IIdentifier> initialId)
		{
			if (initialId.IsDefaultOrEmpty) throw new ArgumentException(message: "Value cannot be empty list.", nameof(initialId));

			_initialId = initialId;
		}

		public void SetName(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			_name = name;
		}

		public void SetBindingType(BindingType bindingType)
		{
			if (bindingType < BindingType.Early || bindingType > BindingType.Late) throw new InvalidEnumArgumentException(nameof(bindingType), (int) bindingType, typeof(BindingType));

			_bindingType = bindingType;
		}

		public void AddState(IState state)
		{
			if (state == null) throw new ArgumentNullException(nameof(state));

			(_states ??= ImmutableArray.CreateBuilder<IStateEntity>()).Add(state);
		}

		public void AddParallel(IParallel parallel)
		{
			if (parallel == null) throw new ArgumentNullException(nameof(parallel));

			(_states ??= ImmutableArray.CreateBuilder<IStateEntity>()).Add(parallel);
		}

		public void AddFinal(IFinal final)
		{
			if (final == null) throw new ArgumentNullException(nameof(final));

			(_states ??= ImmutableArray.CreateBuilder<IStateEntity>()).Add(final);
		}

		public void SetDataModel(IDataModel dataModel) => _dataModel = dataModel ?? throw new ArgumentNullException(nameof(dataModel));

		public void SetScript(IScript script) => _script = script ?? throw new ArgumentNullException(nameof(script));

		public void SetDataModelType(string dataModelType) => _dataModelType = dataModelType ?? throw new ArgumentNullException(nameof(dataModelType));

		public void SetPersistenceLevel(PersistenceLevel persistenceLevel) => _persistenceLevel = persistenceLevel;
		
		public void SetSynchronousEventProcessing(bool value) => _synchronousEventProcessing = value;
		
		public void SetExternalQueueSize(int size) => _externalQueueSize = size;
	}
}