using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace TSSArt.StateMachine
{
	public abstract class StateMachineVisitor
	{
		private readonly StateMachineVisitor                     _masterVisitor;
		private readonly Stack<(object, ImmutableArray<object>)> _path;
		private          StringBuilder                           _errorMessages;

		protected StateMachineVisitor(bool trackPath = false) => _path = trackPath ? new Stack<(object, ImmutableArray<object>)>() : null;

		protected StateMachineVisitor(StateMachineVisitor masterVisitor)
		{
			_masterVisitor = masterVisitor ?? throw new ArgumentNullException(nameof(masterVisitor));

			_path = _masterVisitor?._path;
		}

		private string CurrentPath => _path != null ? string.Join(separator: "/", _path.Reverse().Select(EntityName)) : null;

		private string EntityName((object obj, ImmutableArray<object> array) entry)
		{
			if (entry.array.IsDefault)
			{
				return entry.obj.GetType().Name;
			}

			return ((Type) entry.obj).Name;
		}

		private void Enter<T>(T entity) where T : class => _path?.Push((entity, default));

		private void Enter<T>(ImmutableArray<T> array) where T : class => _path?.Push((typeof(ImmutableArray<T>), array.CastArray<object>()));

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

			_path?.Push((root, default));
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

		private ref struct VisitListData<TIEntity> where TIEntity : class
		{
			public TrackList<TIEntity> List;

			private readonly ImmutableArray<TIEntity> _original;

			public VisitListData(ImmutableArray<TIEntity> list)
			{
				if (list.IsDefault)
				{
					throw new ArgumentNullException(nameof(list));
				}

				_original = list;
				List = new TrackList<TIEntity>(list);
			}

			public void Update(ref ImmutableArray<TIEntity> list)
			{
				if (_original == list && List.IsModified)
				{
					list = List.ModifiedItems.ToImmutable();
				}
			}
		}

		protected ref struct TrackList<T> where T : class
		{
			private readonly ImmutableArray<T> _items;

			public TrackList(ImmutableArray<T> items)
			{
				_items = items;
				ModifiedItems = null;
			}

			public bool IsModified
			{
				get
				{
					if (ModifiedItems == null)
					{
						return false;
					}

					if (_items.Length != ModifiedItems.Count)
					{
						return true;
					}

					for (var i = 0; i < _items.Length; i ++)
					{
						if (!ReferenceEquals(_items[i], ModifiedItems[i]))
						{
							return true;
						}
					}

					return false;
				}
			}

			public ImmutableArray<T>.Builder ModifiedItems { get; private set; }

			public int Count => ModifiedItems?.Count ?? _items.Length;

			public bool Contains(T item) => ModifiedItems?.Contains(item) ?? _items.Contains(item);

			public IEnumerator<T> GetEnumerator() => ModifiedItems != null ? ModifiedItems.GetEnumerator() : ((IEnumerable<T>) _items).GetEnumerator();

			public T this[int index]
			{
				get => ModifiedItems != null ? ModifiedItems[index] : _items[index];
				set
				{
					if (ModifiedItems != null)
					{
						ModifiedItems[index] = value;
					}
					else if (!ReferenceEquals(_items[index], value))
					{
						ModifiedItems = _items.ToBuilder();
						ModifiedItems[index] = value;
					}
				}
			}

			public void Add(T item)
			{
				if (ModifiedItems == null)
				{
					ModifiedItems = _items.ToBuilder();
				}

				ModifiedItems.Add(item);
			}

			public void Clear()
			{
				if (Count == 0)
				{
					return;
				}

				if (ModifiedItems == null)
				{
					ModifiedItems = ImmutableArray<T>.Empty.ToBuilder();
				}
				else
				{
					ModifiedItems.Clear();
				}
			}

			public void Insert(int index, T item)
			{
				if (ModifiedItems == null)
				{
					ModifiedItems = _items.ToBuilder();
				}

				ModifiedItems.Insert(index, item);
			}

			public void RemoveAt(int index)
			{
				if (ModifiedItems == null)
				{
					ModifiedItems = _items.ToBuilder();
				}

				ModifiedItems.RemoveAt(index);
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

		protected virtual void Visit(ref IOutgoingEvent entity)
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

		protected virtual void Visit(ref ICustomAction entity)
		{
			var data = new VisitData<CustomAction, ICustomAction>(entity);
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

		protected virtual void Visit(ref IContentBody entity)
		{
			var data = new VisitData<ContentBody, IContentBody>(entity);
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

				case ICustomAction customAction:
					Visit(ref customAction);
					entity = customAction;
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

		#region Visit(ref ImmutableArray<IT> list)

		protected virtual void Visit(ref ImmutableArray<IIdentifier> list)
		{
			var data = new VisitListData<IIdentifier>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IStateEntity> list)
		{
			var data = new VisitListData<IStateEntity>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<ITransition> list)
		{
			var data = new VisitListData<ITransition>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IEventDescriptor> list)
		{
			var data = new VisitListData<IEventDescriptor>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IHistory> list)
		{
			var data = new VisitListData<IHistory>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IOnEntry> list)
		{
			var data = new VisitListData<IOnEntry>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IOnExit> list)
		{
			var data = new VisitListData<IOnExit>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IExecutableEntity> list)
		{
			var data = new VisitListData<IExecutableEntity>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IInvoke> list)
		{
			var data = new VisitListData<IInvoke>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<ILocationExpression> list)
		{
			var data = new VisitListData<ILocationExpression>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IData> list)
		{
			var data = new VisitListData<IData>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		protected virtual void Visit(ref ImmutableArray<IParam> list)
		{
			var data = new VisitListData<IParam>(list);
			Build(ref list, ref data.List);
			data.Update(ref list);
		}

		#endregion

		#region Build(ref IT entity, ref T properties)

		protected virtual void Build(ref IStateMachine entity, ref StateMachine properties)
		{
			var dataModel = properties.DataModel;
			VisitWrapper(ref dataModel);
			properties.DataModel = dataModel;

			var script = properties.Script;
			VisitWrapper(ref script);
			properties.Script = script;

			var initial = properties.Initial;
			VisitWrapper(ref initial);
			properties.Initial = initial;

			var states = properties.States;
			VisitWrapper(ref states);
			properties.States = states;
		}

		protected virtual void Build(ref ITransition entity, ref Transition properties)
		{
			var Event = properties.Event;
			VisitWrapper(ref Event);
			properties.Event = Event;

			var target = properties.Target;
			VisitWrapper(ref target);
			properties.Target = target;

			var condition = properties.Condition;
			VisitWrapper(ref condition);
			properties.Condition = condition;

			var action = properties.Action;
			VisitWrapper(ref action);
			properties.Action = action;
		}

		protected virtual void Build(ref IState entity, ref State properties)
		{
			var id = properties.Id;
			VisitWrapper(ref id);
			properties.Id = id;

			var dataModel = properties.DataModel;
			VisitWrapper(ref dataModel);
			properties.DataModel = dataModel;

			var initial = properties.Initial;
			VisitWrapper(ref initial);
			properties.Initial = initial;

			var states = properties.States;
			VisitWrapper(ref states);
			properties.States = states;

			var historyStates = properties.HistoryStates;
			VisitWrapper(ref historyStates);
			properties.HistoryStates = historyStates;

			var transitions = properties.Transitions;
			VisitWrapper(ref transitions);
			properties.Transitions = transitions;

			var onEntry = properties.OnEntry;
			VisitWrapper(ref onEntry);
			properties.OnEntry = onEntry;

			var onExit = properties.OnExit;
			VisitWrapper(ref onExit);
			properties.OnExit = onExit;

			var invoke = properties.Invoke;
			VisitWrapper(ref invoke);
			properties.Invoke = invoke;
		}

		protected virtual void Build(ref IParallel entity, ref Parallel properties)
		{
			var id = properties.Id;
			VisitWrapper(ref id);
			properties.Id = id;

			var dataModel = properties.DataModel;
			VisitWrapper(ref dataModel);
			properties.DataModel = dataModel;

			var states = properties.States;
			VisitWrapper(ref states);
			properties.States = states;

			var historyStates = properties.HistoryStates;
			VisitWrapper(ref historyStates);
			properties.HistoryStates = historyStates;

			var transitions = properties.Transitions;
			VisitWrapper(ref transitions);
			properties.Transitions = transitions;

			var onEntry = properties.OnEntry;
			VisitWrapper(ref onEntry);
			properties.OnEntry = onEntry;

			var onExit = properties.OnExit;
			VisitWrapper(ref onExit);
			properties.OnExit = onExit;

			var invoke = properties.Invoke;
			VisitWrapper(ref invoke);
			properties.Invoke = invoke;
		}

		protected virtual void Build(ref IInitial entity, ref Initial properties)
		{
			var transition = properties.Transition;
			VisitWrapper(ref transition);
			properties.Transition = transition;
		}

		protected virtual void Build(ref IFinal entity, ref Final properties)
		{
			var id = properties.Id;
			VisitWrapper(ref id);
			properties.Id = id;

			var onEntry = properties.OnEntry;
			VisitWrapper(ref onEntry);
			properties.OnEntry = onEntry;

			var onExit = properties.OnExit;
			VisitWrapper(ref onExit);
			properties.OnExit = onExit;

			var doneData = properties.DoneData;
			VisitWrapper(ref doneData);
			properties.DoneData = doneData;
		}

		protected virtual void Build(ref IHistory entity, ref History properties)
		{
			var id = properties.Id;
			VisitWrapper(ref id);
			properties.Id = id;

			var transition = properties.Transition;
			VisitWrapper(ref transition);
			properties.Transition = transition;
		}

		protected virtual void Build(ref IOnEntry entity, ref OnEntry properties)
		{
			var action = properties.Action;
			VisitWrapper(ref action);
			properties.Action = action;
		}

		protected virtual void Build(ref IOnExit entity, ref OnExit properties)
		{
			var action = properties.Action;
			VisitWrapper(ref action);
			properties.Action = action;
		}

		protected virtual void Build(ref IAssign entity, ref Assign properties)
		{
			var location = properties.Location;
			VisitWrapper(ref location);
			properties.Location = location;

			var expression = properties.Expression;
			VisitWrapper(ref expression);
			properties.Expression = expression;
		}

		protected virtual void Build(ref ICancel entity, ref Cancel properties)
		{
			var sendIdExpression = properties.SendIdExpression;
			VisitWrapper(ref sendIdExpression);
			properties.SendIdExpression = sendIdExpression;
		}

		protected virtual void Build(ref IElseIf entity, ref ElseIf properties)
		{
			var condition = properties.Condition;
			VisitWrapper(ref condition);
			properties.Condition = condition;
		}

		protected virtual void Build(ref IForEach entity, ref ForEach properties)
		{
			var array = properties.Array;
			VisitWrapper(ref array);
			properties.Array = array;

			var item = properties.Item;
			VisitWrapper(ref item);
			properties.Item = item;

			var index = properties.Index;
			VisitWrapper(ref index);
			properties.Index = index;

			var action = properties.Action;
			VisitWrapper(ref action);
			properties.Action = action;
		}

		protected virtual void Build(ref IIf entity, ref If properties)
		{
			var condition = properties.Condition;
			VisitWrapper(ref condition);
			properties.Condition = condition;

			var action = properties.Action;
			VisitWrapper(ref action);
			properties.Action = action;
		}

		protected virtual void Build(ref ILog entity, ref Log properties)
		{
			var expression = properties.Expression;
			VisitWrapper(ref expression);
			properties.Expression = expression;
		}

		protected virtual void Build(ref IRaise entity, ref Raise properties)
		{
			var @event = properties.Event;
			VisitWrapper(ref @event);
			properties.Event = @event;
		}

		protected virtual void Build(ref IScript entity, ref Script properties)
		{
			var content = properties.Content;
			VisitWrapper(ref content);
			properties.Content = content;

			var source = properties.Source;
			VisitWrapper(ref source);
			properties.Source = source;
		}

		protected virtual void Build(ref ISend entity, ref Send properties)
		{
			var eventExpression = properties.EventExpression;
			VisitWrapper(ref eventExpression);
			properties.EventExpression = eventExpression;

			var targetExpression = properties.TargetExpression;
			VisitWrapper(ref targetExpression);
			properties.TargetExpression = targetExpression;

			var typeExpression = properties.TypeExpression;
			VisitWrapper(ref typeExpression);
			properties.TypeExpression = typeExpression;

			var idLocation = properties.IdLocation;
			VisitWrapper(ref idLocation);
			properties.IdLocation = idLocation;

			var delayExpression = properties.DelayExpression;
			VisitWrapper(ref delayExpression);
			properties.DelayExpression = delayExpression;

			var nameList = properties.NameList;
			VisitWrapper(ref nameList);
			properties.NameList = nameList;

			var parameters = properties.Parameters;
			VisitWrapper(ref parameters);
			properties.Parameters = parameters;

			var content = properties.Content;
			VisitWrapper(ref content);
			properties.Content = content;
		}

		protected virtual void Build(ref IDataModel entity, ref DataModel properties)
		{
			var data = properties.Data;
			VisitWrapper(ref data);
			properties.Data = data;
		}

		protected virtual void Build(ref IData entity, ref Data properties)
		{
			var expression = properties.Expression;
			VisitWrapper(ref expression);
			properties.Expression = expression;

			var source = properties.Source;
			VisitWrapper(ref source);
			properties.Source = source;
		}

		protected virtual void Build(ref IDoneData entity, ref DoneData properties)
		{
			var content = properties.Content;
			VisitWrapper(ref content);
			properties.Content = content;

			var parameters = properties.Parameters;
			VisitWrapper(ref parameters);
			properties.Parameters = parameters;
		}

		protected virtual void Build(ref IInvoke entity, ref Invoke properties)
		{
			var typeExpression = properties.TypeExpression;
			VisitWrapper(ref typeExpression);
			properties.TypeExpression = typeExpression;

			var sourceExpression = properties.SourceExpression;
			VisitWrapper(ref sourceExpression);
			properties.SourceExpression = sourceExpression;

			var idLocation = properties.IdLocation;
			VisitWrapper(ref idLocation);
			properties.IdLocation = idLocation;

			var nameList = properties.NameList;
			VisitWrapper(ref nameList);
			properties.NameList = nameList;

			var content = properties.Content;
			VisitWrapper(ref content);
			properties.Content = content;

			var parameters = properties.Parameters;
			VisitWrapper(ref parameters);
			properties.Parameters = parameters;

			var finalize = properties.Finalize;
			VisitWrapper(ref finalize);
			properties.Finalize = finalize;
		}

		protected virtual void Build(ref IContent entity, ref Content properties)
		{
			var expression = properties.Expression;
			VisitWrapper(ref expression);
			properties.Expression = expression;

			var body = properties.Body;
			VisitWrapper(ref body);
			properties.Body = body;
		}

		protected virtual void Build(ref IParam entity, ref Param properties)
		{
			var expression = properties.Expression;
			VisitWrapper(ref expression);
			properties.Expression = expression;

			var location = properties.Location;
			VisitWrapper(ref location);
			properties.Location = location;
		}

		protected virtual void Build(ref IFinalize entity, ref Finalize properties)
		{
			var action = properties.Action;
			VisitWrapper(ref action);
			properties.Action = action;
		}

		protected virtual void Build(ref ICustomAction entity, ref CustomAction properties) { }

		protected virtual void Build(ref IElse entity, ref Else properties) { }

		protected virtual void Build(ref IValueExpression entity, ref ValueExpression properties) { }

		protected virtual void Build(ref ILocationExpression entity, ref LocationExpression properties) { }

		protected virtual void Build(ref IConditionExpression entity, ref ConditionExpression properties) { }

		protected virtual void Build(ref IScriptExpression entity, ref ScriptExpression properties) { }

		protected virtual void Build(ref IExternalScriptExpression entity, ref ExternalScriptExpression properties) { }

		protected virtual void Build(ref IExternalDataExpression entity, ref ExternalDataExpression properties) { }

		protected virtual void Build(ref IContentBody entity, ref ContentBody properties) { }

		#endregion

		#region Build(ref ImmutableArray<IT> list, ref TrackList<IT> trackList)

		protected virtual void Build(ref ImmutableArray<IIdentifier> list, ref TrackList<IIdentifier> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IStateEntity> list, ref TrackList<IStateEntity> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<ITransition> list, ref TrackList<ITransition> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IEventDescriptor> list, ref TrackList<IEventDescriptor> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IHistory> list, ref TrackList<IHistory> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IOnEntry> list, ref TrackList<IOnEntry> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IOnExit> list, ref TrackList<IOnExit> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IExecutableEntity> list, ref TrackList<IExecutableEntity> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IInvoke> list, ref TrackList<IInvoke> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<ILocationExpression> list, ref TrackList<ILocationExpression> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IData> list, ref TrackList<IData> trackList)
		{
			for (var i = 0; i < trackList.Count; i ++)
			{
				var item = trackList[i];
				VisitWrapper(ref item);
				trackList[i] = item;
			}
		}

		protected virtual void Build(ref ImmutableArray<IParam> list, ref TrackList<IParam> trackList)
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

		private void VisitWrapper(ref IOutgoingEvent entity)
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

		private void VisitWrapper(ref IContentBody entity)
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

		#region VisitWrapper(ref ImmutableArray<IT> entity)

		private void VisitWrapper(ref ImmutableArray<IStateEntity> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<ITransition> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IEventDescriptor> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IIdentifier> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IExecutableEntity> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IHistory> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IOnEntry> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IOnExit> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IInvoke> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<ILocationExpression> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IData> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		private void VisitWrapper(ref ImmutableArray<IParam> entity)
		{
			if (entity.IsDefault) return;
			Enter(entity);
			Visit(ref entity);
			Exit();
		}

		#endregion
	}
}