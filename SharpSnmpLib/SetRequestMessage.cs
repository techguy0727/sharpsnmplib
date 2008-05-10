using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// SET request message.
    /// </summary>
	public class SetRequestMessage: ISnmpMessage, IDisposable
	{
		UdpClient udp = new UdpClient();
		byte[] _bytes;
		ISnmpPdu _pdu;
		VersionCode _version;
		IPAddress _agent;
		string _community;
        IList<Variable> _variables;
        int _sequenceNumber;
        /// <summary>
        /// Creates a <see cref="SetRequestMessage"/> with all contents.
        /// </summary>
        /// <param name="version">Protocol version</param>
        /// <param name="agent">Agent address</param>
        /// <param name="community">Community name</param>
        /// <param name="variables">Variables</param>
		public SetRequestMessage(VersionCode version, IPAddress agent, string community, IList<Variable> variables)
		{
			_version = version;
			_agent = agent;
			_community = community;
            _variables = variables;
            SetRequestPdu pdu = new SetRequestPdu(
                ErrorCode.NoError,
                0,
                _variables);
            _sequenceNumber = pdu.SequenceNumber;
            _bytes = pdu.ToMessageBody(_version, _community).ToBytes();
		}	
        /// <summary>
        /// Sends this <see cref="SetRequestMessage"/> and handles the response from agent.
        /// </summary>
        /// <param name="timeout"></param>
		public void Send(int timeout)
		{
			byte[] bytes = _bytes;
			IPEndPoint agent = new IPEndPoint(_agent, 161);
			udp.Send(bytes,bytes.Length,agent);
			IPEndPoint from = new IPEndPoint(IPAddress.Any,0);
	            IAsyncResult result = udp.BeginReceive(null, this);
	            result.AsyncWaitHandle.WaitOne(timeout, false);
                if (!result.IsCompleted)
                {
                    SharpTimeoutException ex = new SharpTimeoutException();
                    ex.Agent = _agent;
                    ex.Timeout = timeout;
                    throw ex;
                }
	            bytes = udp.EndReceive(result, ref from);
	            MemoryStream m = new MemoryStream(bytes, false);
	        ISnmpMessage message = MessageFactory.ParseMessage(m);
	        if (message.TypeCode != SnmpType.GetResponsePDU) {
                SharpOperationException ex = new SharpOperationException("wrong response");
                ex.Agent = _agent;
                throw ex;
	        }
	        if (((GetResponseMessage)message).SequenceNumber != SequenceNumber) {
                SharpOperationException ex = new SharpOperationException("wrong response");
                ex.Agent = _agent;
                throw ex;
	        }
		}
		/// <summary>
		/// Creates a <see cref="SetRequestMessage"/> with a specific <see cref="SnmpArray"/>.
		/// </summary>
		/// <param name="body">Message body</param>
		public SetRequestMessage(SnmpArray body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            if (body.Items.Count != 3)
            {
                throw new ArgumentException("wrong message body");
            }
            _pdu = (ISnmpPdu)body.Items[2];
            if (_pdu.TypeCode != TypeCode)
            {
                throw new ArgumentException("wrong message type");
            }
            _community = body.Items[1].ToString();
            _version = (VersionCode)((Integer32)body.Items[0]).ToInt32();
            SetRequestPdu pdu = (SetRequestPdu)_pdu;
            _variables = pdu.Variables;
        }
		
		internal int SequenceNumber
		{
			get
			{
				return _sequenceNumber;
			}
		}
	    /// <summary>
	    /// Variables.
	    /// </summary>
        public IList<Variable> Variables
        {
            get
            {
                return _variables;
            }
        }
		/// <summary>
		/// Converts to byte format.
		/// </summary>
		/// <returns></returns>
		public byte[] ToBytes()
		{
			return _bytes;
		}
        /// <summary>
        /// PDU.
        /// </summary>
		public ISnmpPdu Pdu {
			get {
				return _pdu;
			}
		}
		/// <summary>
		/// Type code.
		/// </summary>
		public SnmpType TypeCode {
			get {
				return SnmpType.SetRequestPDU;
			}
		}

		private bool _disposed;
		/// <summary>
		/// Finalizer of <see cref="SetRequestMessage"/>.
		/// </summary>
		~SetRequestMessage()
		{
			Dispose(false);
		}
		/// <summary>
		/// Releases all resources used by the <see cref="SetRequestMessage"/>.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// Disposes of the resources (other than memory) used by the <see cref="SetRequestMessage"/>.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources. 
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) {
				return;
			}
			if (disposing) {
				(udp as IDisposable).Dispose();		
			}
			_disposed = true;
		}		
	}
}