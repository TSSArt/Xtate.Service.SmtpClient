using System;

namespace TSSArt.StateMachine
{
	public class CancelBuilder : ICancelBuilder
	{
		private string           _sendId;
		private IValueExpression _sendIdExpression;

		public ICancel Build()
		{
			if (_sendId != null && _sendIdExpression != null)
			{
				throw new InvalidOperationException(message: "SendId and SendIdExpression can't be used at the same time in Cancel element");
			}

			return new Cancel { SendId = _sendId, SendIdExpression = _sendIdExpression };
		}

		public void SetSendId(string sendId)
		{
			if (string.IsNullOrEmpty(sendId)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(sendId));

			_sendId = sendId;
		}

		public void SetSendIdExpression(IValueExpression sendIdExpression) => _sendIdExpression = sendIdExpression ?? throw new ArgumentNullException(nameof(sendIdExpression));
	}
}