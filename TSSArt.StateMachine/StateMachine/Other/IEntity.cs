namespace TSSArt.StateMachine
{
	public interface IEntity { }

	public interface IEntity<TEntity, in TIEntity> where TEntity : struct, IEntity<TEntity, TIEntity>, TIEntity where TIEntity : class
	{
		void Init(TIEntity source);
		bool RefEquals(in TEntity other);
	}
}