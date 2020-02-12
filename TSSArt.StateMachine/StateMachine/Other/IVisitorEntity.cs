namespace TSSArt.StateMachine
{
	internal interface IVisitorEntity<TEntity, in TIEntity> where TEntity : struct, IVisitorEntity<TEntity, TIEntity>, TIEntity where TIEntity : class
	{
		void Init(TIEntity source);
		bool RefEquals(in TEntity other);
	}
}