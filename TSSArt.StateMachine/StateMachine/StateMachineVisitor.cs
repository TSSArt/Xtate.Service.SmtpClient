using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSSArt.StateMachine
{
	public abstract class StateMachineVisitor
	{
		private readonly StateMachineVisitor _masterVisitor;
		private readonly Stack<object>       _path;
		private          StringBuilder       _errorMessages;

		protected StateMachineVisitor(bool trackPath = false) => _path = trackPath ? new Stack<object>() : null;

		protected StateMachineVisitor(StateMachineVisitor masterVisitor)
		{
			_masterVisitor = masterVisitor ?? throw new ArgumentNullException(nameof(masterVisitor));

			_path = _masterVisitor?._path;
		}

		private string CurrentPath => _path != null ? string.Join(separator: "/", _path.Reverse().Select(EntityName)) : null;

		private string EntityName(object entity) => entity.GetType().Name;

		private void Enter<T>(T entity) where T : class => _path?.Push(entity);

		private void Exit() => _path?.Pop();

		protected void SetRootPath(object root)
		{
			if (root == null) throw new ArgumentNullException(nameof(root));

			if (_masterVisitor != null)
			{
				throw new InvalidOperationException(message: "Only root visitor can call SetRootPath() method.");
			}

			if (_path?.Count > 0)
			{
				throw new InvalidOperationException(message: "Root path can be set only before visiting.");
			}

			_path?.Push(root);
		}

		protected void AddErrorMessage(string message)
		{
			if (_masterVisitor != null)
			{
				_masterVisitor.AddErrorMessage(message);
			}
			else
			{
				if (_errorMessages == null)
				{
					_errorMessages = new StringBuilder();
				}
				else
				{
					_errorMessages.AppendLine();
				}

				_errorMessages.Append(message);

				if (_path != null)
				{
					_errorMessages.Append(value: " Path: ").Append(CurrentPath);
				}
			}
		}

		protected void ThrowIfErrors()
		{
			if (_masterVisitor != null)
			{
				throw new InvalidOperationException(message: "Only root visitor can call ThrowIfErrors() method");
			}

			if (_errorMessages != null)
			{
				throw new InvalidOperationException(_errorMessages.ToString());
			}
		}

		private ref struct VisitData<TEntity, TIEntity> where TEntity : struct, IEntity<TEntity, TIEntity>, TIEntity where TIEntity : class
		{
			public TEntity Properties;

			private          TEntity  _original;
			private readonly TIEntity _entity;

			public VisitData(TIEntity entity)
			{
				_entity = entity ?? throw new ArgumentNullException(nameof(entity));

				if (entity is TEntity tmp)
				{
					_original = tmp;
				}
				else
				{
					_original = new TEntity();
					_original.Init(entity);
				}

				Properties = _original;
			}

			public void Update(ref TIEntity entity)
			{
				if (ReferenceEquals(_entity, entity) && !_original.RefEquals(in Properties))
				{
					entity = Properties;
				}
			}
		}

		private ref struct VisitListData<TList, TIEntity> where TList : ValidatedReadOnlyList<TList, TIEntity>, new() where TIEntity : class
		{
			public TrackList<TIEntity> List;

			private readonly IReadOnlyList<TIEntity> _original;

			public VisitListData(IReadOnlyList<TIEntity> list)
			{
				_original = list ?? throw new ArgumentNullException(nameof(list));
				List = new TrackList<TIEntity>(list);
			}

			public void Update(ref IReadOnlyList<TIEntity> list)
			{
				if (ReferenceEquals(_original, list) && List.IsModified)
				{
					list = ValidatedReadOnlyList<TList, TIEntity>.Create(List.Items);
				}
			}
		}

		protected ref struct TrackList<T>
		{
			private readonly IReadOnlyList<T> _items;
			private          List<T>          _newItems;

			public TrackList(IReadOnlyList<T> items)
			{
				_items = items;
				_newItems = null;
			}

			public bool IsModified
			{
				get
				{
					if (_newItems == null)
					{
						return false;
					}

					if (_items.Count != _newItems.Count)
					{
						return true;
					}

					for (var i = 0; i < _items.Count; i ++)
					{
						if (!ReferenceEquals(_items[i], _newItems[i]))
						{
							return true;
						}
					}

					return false;
				}
			}

			public IReadOnlyList<T> Items => _newItems ?? _items;

			public int Count => Items.Count;

			public bool Contains(T item) => Items.Contains(item);

			public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

			public T this[int index]
			{
				get => Items[index];
				set
				{
					if (_newItems != null)
					{
						_newItems[index] = value;
					}
					else if (!ReferenceEquals(_items[index], value))
					{
						_newItems = new List<T>(_items) { [index] = value };
					}
				}
			}

			public void Add(T item)
			{
				if (_newItems == null)
				{
					_newItems = new List<T>(_items);
				}

				_newItems.Add(item);
			}

			public void Clear()
			{
				if (Count == 0)
				{
					return;
				}

				if (_newItems == null)
				{
					_newItems = new List<T>();
				}
				else
				{
					_newItems.Clear();
				}
			}

			public void Insert(int index, T item)
			{
				if (_newItems == null)
				{
					_newItems = new List<T>(_items);
				}

				_newItems.Insert(index, item);
			}

			public void RemoveAt(int index)
			{
				if (_newItems == null)
				{
					_newItems = new List<T>(_items);
				}

				_newItems.RemoveAt(index);
			}
		}

		#region Visit(ref IT entity)

		protected virtual void Visit(ref IIdentifier entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));
		}

		protected virtual void Visit(ref IEventDescriptor entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));
		}

		protected virtual void Visit(ref IEvent entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));
		}

		protected virtual void Visit(ref IStateMachine entity)
		{
			var data = new VisitData<StateMachine, IStateMachine>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref ITransition entity)
		{
			var data = new VisitData<Transition, ITransition>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IState entity)
		{
			var data = new VisitData<State, IState>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IParallel entity)
		{
			var data = new VisitData<Parallel, IParallel>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IInitial entity)
		{
			var data = new VisitData<Initial, IInitial>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IFinal entity)
		{
			var data = new VisitData<Final, IFinal>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IHistory entity)
		{
			var data = new VisitData<History, IHistory>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IOnEntry entity)
		{
			var data = new VisitData<OnEntry, IOnEntry>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IOnExit entity)
		{
			var data = new VisitData<OnExit, IOnExit>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IAssign entity)
		{
			var data = new VisitData<Assign, IAssign>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref ICancel entity)
		{
			var data = new VisitData<Cancel, ICancel>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IElseIf entity)
		{
			var data = new VisitData<ElseIf, IElseIf>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IForEach entity)
		{
			var data = new VisitData<ForEach, IForEach>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IIf entity)
		{
			var data = new VisitData<If, IIf>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref ILog entity)
		{
			var data = new VisitData<Log, ILog>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IRaise entity)
		{
			var data = new VisitData<Raise, IRaise>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IScript entity)
		{
			var data = new VisitData<Script, IScript>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref ISend entity)
		{
			var data = new VisitData<Send, ISend>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IDataModel entity)
		{
			var data = new VisitData<DataModel, IDataModel>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IData entity)
		{
			var data = new VisitData<Data, IData>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IDoneData entity)
		{
			var data = new VisitData<DoneData, IDoneData>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IInvoke entity)
		{
			var data = new VisitData<Invoke, IInvoke>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IContent entity)
		{
			var data = new VisitData<Content, IContent>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IParam entity)
		{
			var data = new VisitData<Param, IParam>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IFinalize entity)
		{
			var data = new VisitData<Finalize, IFinalize>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IElse entity)
		{
			var data = new VisitData<Else, IElse>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IValueExpression entity)
		{
			var data = new VisitData<ValueExpression, IValueExpression>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref ILocationExpression entity)
		{
			var data = new VisitData<LocationExpression, ILocationExpression>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IConditionExpression entity)
		{
			var data = new VisitData<ConditionExpression, IConditionExpression>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IScriptExpression entity)
		{
			var data = new VisitData<ScriptExpression, IScriptExpression>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IExternalScriptExpression entity)
		{
			var data = new VisitData<ExternalScriptExpression, IExternalScriptExpression>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IExternalDataExpression entity)
		{
			var data = new VisitData<ExternalDataExpression, IExternalDataExpression>(entity);
			Build(ref entity, ref data.Properties);
			data.Update(ref entity);
		}

		protected virtual void Visit(ref IExecutableEntity entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			switch (entity)
			{
				case IAssign assign:
					Visit(ref assign);
					entity = assign;
					break;

				case ICancel cancel:
					Visit(ref cancel);
					entity = cancel;
					break;

				case IConditionExpression conditionExpression:
					Visit(ref conditionExpression);
					entity = conditionExpression;
					break;

				case IElse @else:
					Visit(ref @else);
					entity = @else;
					break;

				case IElseIf elseIf:
					Visit(ref elseIf);
					entity = elseIf;
					break;

				case IForEach forEach:
					Visit(ref forEach);
					entity = forEach;
					break;

				case IIf @if:
					Visit(ref @if);
					entity = @if;
					break;

				case ILog log:
					Visit(ref log);
					entity = log;
					break;

				case IRaise raise:
					Visit(ref raise);
					entity = raise;
					break;

				case IScript script:
					Visit(ref script);
					entity = script;
					break;

				case ISend send:
					Visit(ref send);
					entity = send;
					break;

				default:
					VisitUnknown(ref entity);
					break;
			}
		}

		protected virtual void VisitUnknown(ref IExecutableEntity entity) { }

		protected virtual void Visit(ref IStateEntity entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			switch (entity)
			{
				case IState state:
					Visit(ref state);
					entity = state;
					break;

				case IParallel parallel:
					Visit(ref parallel);
					entity = parallel;
					break;

				case IFinal final:
					Visit(ref final);
					entity = final;
					break;

				default:
					VisitUnknown(ref entity);
					break;
			}
		}

		protected virtual void VisitUnknown(ref IStateEntity entity) { }

		#endregion

		#region Visit(ref IReadOnlyList<IT> list)

		protected virtual void Visit(ref IReadOnlyList<IIdentifier> list)
		{
			var data = new VisitListData<IdentifierList, IIdentifier>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IStateEntity> list)
		{
			var data = new VisitListData<StateEntityList, IStateEntity>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<ITransition> list)
		{
			var data = new VisitListData<TransitionList, ITransition>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IEventDescriptor> list)
		{
			var data = new VisitListData<EventDescriptorList, IEventDescriptor>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IHistory> list)
		{
			var data = new VisitListData<HistoryList, IHistory>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IOnEntry> list)
		{
			var data = new VisitListData<OnEntryList, IOnEntry>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IOnExit> list)
		{
			var data = new VisitListData<OnExitList, IOnExit>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IExecutableEntity> list)
		{
			var data = new VisitListData<ExecutableEntityList, IExecutableEntity>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IInvoke> list)
		{
			var data = new VisitListData<InvokeList, IInvoke>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<ILocationExpression> list)
		{
			var data = new VisitListData<LocationExpressionList, ILocationExpression>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IData> list)
		{
			var data = new VisitListData<DataList, IData>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref IReadOnlyList<IParam> list)
		{
			var data = new VisitListData<ParamList, IParam>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		#endregion

		#region Build(ref IT entity, ref T properties)

		protected virtual void Build(ref IStateMachine entity, ref StateMachine properties)
		{
			VisitWrapper(ref properties.DataModel);
			VisitWrapper(ref properties.Script);
			VisitWrapper(ref properties.Initial);
			VisitWrapper(ref properties.States);
		}

		protected virtual void Build(ref ITransition entity, ref Transition properties)
		{
			VisitWrapper(ref properties.Event);
			VisitWrapper(ref properties.Target);
			VisitWrapper(ref properties.Condition);
			VisitWrapper(ref properties.Action);
		}

		protected virtual void Build(ref IState entity, ref State properties)
		{
			VisitWrapper(ref properties.Id);
			VisitWrapper(ref properties.DataModel);
			VisitWrapper(ref properties.Initial);
			VisitWrapper(ref properties.States);
			VisitWrapper(ref properties.HistoryStates);
			VisitWrapper(ref properties.Transitions);
			VisitWrapper(ref properties.OnEntry);
			VisitWrapper(ref properties.OnExit);
			VisitWrapper(ref properties.Invoke);
		}

		protected virtual void Build(ref IParallel entity, ref Parallel properties)
		{
			VisitWrapper(ref properties.Id);
			VisitWrapper(ref properties.DataModel);
			VisitWrapper(ref properties.States);
			VisitWrapper(ref properties.HistoryStates);
			VisitWrapper(ref properties.Transitions);
			VisitWrapper(ref properties.OnEntry);
			VisitWrapper(ref properties.OnExit);
			VisitWrapper(ref properties.Invoke);
		}

		protected virtual void Build(ref IInitial entity, ref Initial properties)
		{
			VisitWrapper(ref properties.Transition);
		}

		protected virtual void Build(ref IFinal entity, ref Final properties)
		{
			VisitWrapper(ref properties.Id);
			VisitWrapper(ref properties.OnEntry);
			VisitWrapper(ref properties.OnExit);
			VisitWrapper(ref properties.DoneData);
		}

		protected virtual void Build(ref IHistory entity, ref History properties)
		{
			VisitWrapper(ref properties.Id);
			VisitWrapper(ref properties.Transition);
		}

		protected virtual void Build(ref IOnEntry entity, ref OnEntry properties)
		{
			VisitWrapper(ref properties.Action);
		}

		protected virtual void Build(ref IOnExit entity, ref OnExit properties)
		{
			VisitWrapper(ref properties.Action);
		}

		protected virtual void Build(ref IAssign entity, ref Assign properties)
		{
			VisitWrapper(ref properties.Location);
			VisitWrapper(ref properties.Expression);
		}

		protected virtual void Build(ref ICancel entity, ref Cancel properties)
		{
			VisitWrapper(ref properties.SendIdExpression);
		}

		protected virtual void Build(ref IElseIf entity, ref ElseIf properties)
		{
			VisitWrapper(ref properties.Condition);
		}

		protected virtual void Build(ref IForEach entity, ref ForEach properties)
		{
			VisitWrapper(ref properties.Array);
			VisitWrapper(ref properties.Item);
			VisitWrapper(ref properties.Index);
			VisitWrapper(ref properties.Action);
		}

		protected virtual void Build(ref IIf entity, ref If properties)
		{
			VisitWrapper(ref properties.Condition);
			VisitWrapper(ref properties.Action);
		}

		protected virtual void Build(ref ILog entity, ref Log properties)
		{
			VisitWrapper(ref properties.Expression);
		}

		protected virtual void Build(ref IRaise entity, ref Raise properties)
		{
			VisitWrapper(ref properties.Event);
		}

		protected virtual void Build(ref IScript entity, ref Script properties)
		{
			VisitWrapper(ref properties.Content);
			VisitWrapper(ref properties.Source);
		}

		protected virtual void Build(ref ISend entity, ref Send properties)
		{
			VisitWrapper(ref properties.EventExpression);
			VisitWrapper(ref properties.TargetExpression);
			VisitWrapper(ref properties.TypeExpression);
			VisitWrapper(ref properties.IdLocation);
			VisitWrapper(ref properties.DelayExpression);
			VisitWrapper(ref properties.NameList);
			VisitWrapper(ref properties.Parameters);
			VisitWrapper(ref properties.Content);
		}

		protected virtual void Build(ref IDataModel entity, ref DataModel properties)
		{
			VisitWrapper(ref properties.Data);
		}

		protected virtual void Build(ref IData entity, ref Data properties)
		{
			VisitWrapper(ref properties.Expression);
			VisitWrapper(ref properties.Source);
		}

		protected virtual void Build(ref IDoneData entity, ref DoneData properties)
		{
			VisitWrapper(ref properties.Content);
			VisitWrapper(ref properties.Parameters);
		}

		protected virtual void Build(ref IInvoke entity, ref Invoke properties)
		{
			VisitWrapper(ref properties.TypeExpression);
			VisitWrapper(ref properties.SourceExpression);
			VisitWrapper(ref properties.IdLocation);
			VisitWrapper(ref properties.NameList);
			VisitWrapper(ref properties.Content);
			VisitWrapper(ref properties.Parameters);
			VisitWrapper(ref properties.Finalize);
		}

		protected virtual void Build(ref IContent entity, ref Content properties)
		{
			VisitWrapper(ref properties.Expression);
		}

		protected virtual void Build(ref IParam entity, ref Param properties)
		{
			VisitWrapper(ref properties.Expression);
			VisitWrapper(ref properties.Location);
		}

		protected virtual void Build(ref IFinalize entity, ref Finalize properties)
		{
			VisitWrapper(ref properties.Action);
		}

		protected virtual void Build(ref IElse entity, ref Else properties) { }

		protected virtual void Build(ref IValueExpression entity, ref ValueExpression properties) { }

		protected virtual void Build(ref ILocationExpression entity, ref LocationExpression properties) { }

		protected virtual void Build(ref IConditionExpression entity, ref ConditionExpression properties) { }

		protected virtual void Build(ref IScriptExpression entity, ref ScriptExpression properties) { }

		protected virtual void Build(ref IExternalScriptExpression entity, ref ExternalScriptExpression properties) { }

		protected virtual void Build(ref IExternalDataExpression entity, ref ExternalDataExpression properties) { }

		#endregion

		#region Build(ref IReadOnlyList<IT> list, ref TrackList<IT> trackList)

		protected virtual void Build(ref IReadOnlyList<IIdentifier> list, ref TrackList<IIdentifier> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IStateEntity> list, ref TrackList<IStateEntity> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<ITransition> list, ref TrackList<ITransition> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IEventDescriptor> list, ref TrackList<IEventDescriptor> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IHistory> list, ref TrackList<IHistory> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IOnEntry> list, ref TrackList<IOnEntry> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IOnExit> list, ref TrackList<IOnExit> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IExecutableEntity> list, ref TrackList<IExecutableEntity> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IInvoke> list, ref TrackList<IInvoke> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<ILocationExpression> list, ref TrackList<ILocationExpression> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IData> list, ref TrackList<IData> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref IReadOnlyList<IParam> list, ref TrackList<IParam> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		#endregion

		#region VisitWrapper(ref IT entity)

		private void VisitWrapper(ref IExecutableEntity entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IInitial entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IIdentifier entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IStateEntity entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IEventDescriptor entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ITransition entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IDoneData entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IHistory entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IOnEntry entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IOnExit entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IConditionExpression entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ILocationExpression entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IValueExpression entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IExternalDataExpression entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IScriptExpression entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IExternalScriptExpression entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IEvent entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IInvoke entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IDataModel entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IData entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IParam entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IContent entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IFinalize entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		#endregion

		#region VisitWrapper(ref IReadOnlyList<IT> entity)

		private void VisitWrapper(ref IReadOnlyList<IStateEntity> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<ITransition> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IEventDescriptor> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IIdentifier> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IExecutableEntity> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IHistory> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IOnEntry> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IOnExit> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IInvoke> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<ILocationExpression> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IData> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref IReadOnlyList<IParam> entity)
		{
			if (entity == null) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		#endregion
	}
}