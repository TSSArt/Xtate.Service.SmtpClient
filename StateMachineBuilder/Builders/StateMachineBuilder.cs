using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TSSArt.StateMachine
{
	public class StateMachineBuilder : IStateMachineBuilder
	{
		private readonly List<IStateEntity>         _states = new List<IStateEntity>();
		private          BindingType                _bindingType;
		private          IDataModel                 _dataModel;
		private          string                     _dataModelType;
		private          IReadOnlyList<IIdentifier> _initialId;
		private          string                     _name;
		private          IScript                    _script;

		public IStateMachine Build()
		{
			var initial = _initialId != null ? (IInitial) new Initial { Transition = new Transition { Target = _initialId } } : null;

			if (initial != null && _states.Count == 0)
			{
				throw new InvalidOperationException(message: "Initial state/property cannot be used without any states");
			}

			IStateMachine stateMachine = new StateMachine
										 {
												 Name = _name, Initial = initial, DataModelType = _dataModelType, Binding = _bindingType,
												 States = StateEntityList.Create(_states), DataModel = _dataModel, Script = _script
										 };

			NoneDataModelHandler.Validate(stateMachine);
			RuntimeDataModelHandler.Validate(stateMachine);

			return stateMachine;
		}

		public void SetInitial(IReadOnlyList<IIdentifier> initial)
		{
			_initialId = IdentifierList.Create(initial ?? throw new ArgumentNullException(nameof(initial)));
		}

		public void SetName(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			_name = name;
		}

		public void SetBindingType(BindingType bindingType)
		{
			if (!Enum.IsDefined(typeof(BindingType), bindingType)) throw new InvalidEnumArgumentException(nameof(bindingType), (int) bindingType, typeof(BindingType));

			_bindingType = bindingType;
		}

		public void AddState(IState state)
		{
			if (state == null) throw new ArgumentNullException(nameof(state));

			_states.Add(state);
		}

		public void AddParallel(IParallel parallel)
		{
			if (parallel == null) throw new ArgumentNullException(nameof(parallel));

			_states.Add(parallel);
		}

		public void AddFinal(IFinal final)
		{
			if (final == null) throw new ArgumentNullException(nameof(final));

			_states.Add(final);
		}

		public void SetDataModel(IDataModel dataModel) => _dataModel = dataModel ?? throw new ArgumentNullException(nameof(dataModel));

		public void SetScript(IScript script) => _script = script ?? throw new ArgumentNullException(nameof(script));

		public void SetDataModelType(string dataModelType) => _dataModelType = dataModelType ?? throw new ArgumentNullException(nameof(dataModelType));
	}
}