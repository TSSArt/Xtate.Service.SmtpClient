using System.Diagnostics.Contracts;

namespace TSSArt.StateMachine
{
	internal interface IVisitorEntity<TEntity, in TIEntity> where TEntity : struct, IVisitorEntity<TEntity, TIEntity>, TIEntity
	{
		void Init(TIEntity source);

		[Pure]
		bool RefEquals(in TEntity other);
	}
}