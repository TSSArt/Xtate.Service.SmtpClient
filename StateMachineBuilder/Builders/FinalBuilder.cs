using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class FinalBuilder : IFinalBuilder
	{
		private readonly List<IOnEntry> _onEntryList = new List<IOnEntry>();
		private readonly List<IOnExit>  _onExitList  = new List<IOnExit>();
		private          IDoneData      _doneData;
		private          IIdentifier    _id;

		public IFinal Build() => new Final { Id = _id, OnEntry = OnEntryList.Create(_onEntryList), OnExit = OnExitList.Create(_onExitList), DoneData = _doneData };

		public void SetId(IIdentifier id) => _id = id ?? throw new ArgumentNullException(nameof(id));

		public void AddOnEntry(IOnEntry onEntry)
		{
			if (onEntry == null) throw new ArgumentNullException(nameof(onEntry));

			_onEntryList.Add(onEntry);
		}

		public void AddOnExit(IOnExit onExit)
		{
			if (onExit == null) throw new ArgumentNullException(nameof(onExit));

			_onExitList.Add(onExit);
		}

		public void SetDoneData(IDoneData doneData) => _doneData = doneData ?? throw new ArgumentNullException(nameof(doneData));
	}
}