using System;
using System.IO;
using System.Runtime.InteropServices;
using static System.Collections.Specialized.BitVector32;

namespace GnuTlsWrap {
	internal static class Static {

		public const string Ciphers = @"SECURE256:+SECURE128:-ARCFOUR-128:-3DES-CBC:-MD5:+SIGN-ALL:-SIGN-RSA-MD5:+CTYPE-X509:-VERS-SSL3.0:-VERS-TLS1.3";

		// G l o b a l

		public static string? CheckVersion(string? reqVersion) {
			return Marshal.PtrToStringAnsi(gnutls_check_version(reqVersion));
		}
		// const char * gnutls_check_version (const char * req_version)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_check_version")]
		private static extern IntPtr gnutls_check_version([In()][MarshalAs(UnmanagedType.LPStr)] string? req_version);

		// This is the function where you set the logging function
		// gnutls is going to use.This function only accepts a
		// character array.Normally you may not use this function
		// since it is only used for debugging purposes.
		public static void GlobalSetLogFunction(Logging.GnuTlsLogCBFunc logCBFunc) {
			gnutls_global_set_log_function(logCBFunc);
		}
		// void gnutls_global_set_log_function (gnutls_log_func log_func)
		// gnutls_log_func is of the form: void (*gnutls_log_func)( int level, const char*);
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_global_set_log_function")]
		private static extern void gnutls_global_set_log_function([In()][MarshalAs(UnmanagedType.FunctionPtr)] Logging.GnuTlsLogCBFunc log_func);

		// This is the function that allows you to set the log level.
		// The level is an integer between 0 and 99. Higher values mean
		// more verbosity. The default value is 0. Larger values should
		// only be used with care, since they may reveal sensitive information.
		// Use a log level over 10 to enable all debugging options.
		public static void GlobalSetLogLevel(int level) {
			gnutls_global_set_log_level(level);
		}
		// void gnutls_global_set_log_level (int level)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_global_set_log_level")]
		private static extern void gnutls_global_set_log_level(int level);

		public static int GlobalInit() {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_global_init());
		}
		// int gnutls_global_init ()
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_global_init")]
		private static extern int gnutls_global_init();

		public static void GlobalDeInit() {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			gnutls_global_deinit();
		}
		// void gnutls_global_deinit ()
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_global_deinit")]
		private static extern void gnutls_global_deinit();

		public static void Free(IntPtr ptr) {
			//gnutls_free(ptr);
		}
		// void gnutls_free(* ptr)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_free")]
		public static extern void gnutls_free(IntPtr ptr);


		// S e s s i o n

		public static void DbSetCacheExpiration(Session sess, int seconds) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			gnutls_db_set_cache_expiration(sess.ptr, seconds);
			return;
		}
		// void gnutls_db_set_cache_expiration (gnutls_session_t session, int seconds)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_db_set_cache_expiration")]
		public static extern void gnutls_db_set_cache_expiration(IntPtr session, int seconds);

		// Info

		public static string SessionGetDesc(Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			IntPtr descPtr = gnutls_session_get_desc(sess.ptr);
			string desc = Marshal.PtrToStringAnsi(descPtr);
			//Static.gnutls_free(descPtr);
			return desc;
		}
		// char* gnutls_session_get_desc(gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_session_get_desc")]
		private static extern IntPtr gnutls_session_get_desc(IntPtr session);

		public static string ProtocolGetName(ProtocolT version) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			IntPtr namePtr = gnutls_protocol_get_name(version);
			string name = Marshal.PtrToStringAnsi(namePtr);
			//Static.gnutls_free(namePtr);
			return name;
		}
		// const char * gnutls_protocol_get_name (gnutls_protocol_t version)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_protocol_get_name")]
		private static extern IntPtr gnutls_protocol_get_name(ProtocolT version);

		public static ProtocolT ProtocolGetVersion(Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return (ProtocolT)Utils.Check(gcm, (int)gnutls_protocol_get_version(sess.ptr));
		}
		// gnutls_protocol_t gnutls_protocol_get_version (gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_protocol_get_version")]
		private static extern ProtocolT gnutls_protocol_get_version(IntPtr session);

		public static int RecordGetMaxSize(Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return gnutls_record_get_max_size(sess.ptr);
		}
		// size_t gnutls_record_get_max_size (gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_record_get_max_size")]
		private static extern int gnutls_record_get_max_size(IntPtr session);

		// Traffic

		public static int HandShake(Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_handshake(sess.ptr));
		}
		// int gnutls_handshake (gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_handshake")]
		private static extern int gnutls_handshake(IntPtr session);

		public static void HandshakeSetHookFunction(Session sess, int htype, int when, FtpGnuLib.FtpGnuStream.GnuTlsHandshakeHookFunc handshakeHookFunc) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			gnutls_handshake_set_hook_function(sess.ptr, htype, when, handshakeHookFunc);
		}
		// void gnutls_handshake_set_hook_function (gnutls_session_t session, unsigned int htype, int when, gnutls_handshake_hook_func func)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_handshake_set_hook_function")]
		private static extern void gnutls_handshake_set_hook_function(IntPtr session, int htype, int when, [In()][MarshalAs(UnmanagedType.FunctionPtr)] FtpGnuLib.FtpGnuStream.GnuTlsHandshakeHookFunc func);

		public static int Bye(Session sess, CloseRequestT how) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_bye(sess.ptr, how));
		}
		// int gnutls_bye (gnutls_session_t session, gnutls_close_request_t how)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_bye")]
		private static extern int gnutls_bye(IntPtr session, CloseRequestT how);

		public static void HandshakeSetTimeout(Session sess, uint ms) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			gnutls_handshake_set_timeout(sess.ptr, ms);
		}
		// void gnutls_handshake_set_timeout (gnutls_session_t session, unsigned int ms)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_handshake_set_timeout")]
		private static extern void gnutls_handshake_set_timeout(IntPtr session, uint ms);

		public static int RecordCheckPending(Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return gnutls_record_check_pending(sess.ptr);
		}
		// size_t gnutls_record_check_pending (gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_record_check_pending")]
		private static extern int gnutls_record_check_pending(IntPtr session);


		// Priorities

		public static int SetDefaultPriority(Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_set_default_priority(sess.ptr));
		}
		// int gnutls_set_default_priority (gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_set_default_priority")]
		private static extern int gnutls_set_default_priority(IntPtr session);

		public static int PrioritySetDirect(Session sess, string priorities) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			IntPtr errPos; // does not seem terribly useful...
			return Utils.Check(gcm, gnutls_priority_set_direct(sess.ptr, priorities, out errPos));
		}
		// int gnutls_priority_set_direct(gnutls_session_t session, const char* priorities, const char** err_pos)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_priority_set_direct")]
		private static extern int gnutls_priority_set_direct(IntPtr session, [In()][MarshalAs(UnmanagedType.LPStr)] string? priorities, out IntPtr err_pos);

		public static int SetDefaultPriorityAppend(Session sess, string priorities) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			IntPtr errPos; // does not seem terribly useful...
			return Utils.Check(gcm, gnutls_set_default_priority_append(sess.ptr, priorities, out errPos, 0));
		}
		// int gnutls_set_default_priority_append (gnutls_session_t session, const char * add_prio, const char ** err_pos, unsigned flags)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_set_default_priority_append")]
		private static extern int gnutls_set_default_priority_append(IntPtr session, [In()][MarshalAs(UnmanagedType.LPStr)] string? priorities, out IntPtr err_pos, uint flags);

		public static int DhSetPrimeBits(Session sess, uint bits) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_dh_set_prime_bits(sess.ptr, bits));
		}
		// void gnutls_dh_set_prime_bits (gnutls_session_t session, unsigned int bits)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_dh_set_prime_bits")]
		private static extern int gnutls_dh_set_prime_bits(IntPtr session, uint bits);

		// Transport

		public static int TransportSetInt(Session sess, int socketDescriptor) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_transport_set_ptr(sess.ptr, socketDescriptor));
		}
		// void gnutls_transport_set_int (gnutls_session_t session, int fd)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_transport_set_ptr")]
		private static extern int gnutls_transport_set_ptr(IntPtr session, int fd);

		// ssize_t gnutls_record_recv (gnutls_session_t session, void * data, size_t data_size)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_record_recv")]
		internal static extern int gnutls_record_recv(IntPtr session, [Out()][MarshalAs(UnmanagedType.LPArray, SizeConst = 2048)] byte[] data, int data_size);

		// ssize_t gnutls_record_send (gnutls_session_t session, const void * data, size_t data_size)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_record_send")]
		internal static extern int gnutls_record_send(IntPtr session, [In()][MarshalAs(UnmanagedType.LPArray, SizeConst = 2048)] byte[] data, int data_size);
		// ssize_t gnutls_record_send (gnutls_session_t session, const void * data, size_t data_size)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_record_send")]
		internal static extern int gnutls_record_sendnull(IntPtr session, IntPtr data, int data_size);

		// Session Resume

		public static int SessionIsResumed(Session sess) {
			return Utils.Check(Utils.GetCurrentMethod(), gnutls_session_is_resumed(sess.ptr));
		}
		// int gnutls_session_is_resumed (gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_session_is_resumed")]
		private static extern int gnutls_session_is_resumed(IntPtr session);

		public static int SessionGetData2(Session sess, ref DatumT data) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_session_get_data2(sess.ptr, data));
		}
		// int gnutls_session_get_data2 (gnutls_session_t session, gnutls_datum_t * data)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_session_get_data2")]
		private static extern int gnutls_session_get_data2(IntPtr session, [MarshalAs(UnmanagedType.LPStruct)] DatumT data);

		public static int SessionSetData(Session sess, DatumT data) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_session_set_data(sess.ptr, data.ptr, data.size));
		}
		// int gnutls_session_set_data (gnutls_session_t session, const void * session_data, size_t session_data_size)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_session_set_data")]
		private static extern int gnutls_session_set_data(IntPtr session, IntPtr session_data, uint session_data_size);

		// const gnutls_datum_t* gnutls_certificate_get_peers (gnutls_session_t session, unsigned int * list_size)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_certificate_get_peers")]
		private static extern IntPtr gnutls_certificate_get_peers(IntPtr session, IntPtr session_data, uint list_size);

		// ALPN

		public static int AlpnSetProtocols(Session sess, string protocols) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			var datumPtr = Marshal.AllocHGlobal(Marshal.SizeOf<DatumT>());
			var valuePtr = Marshal.StringToHGlobalAnsi(protocols);
			Marshal.StructureToPtr(new DatumT { ptr = valuePtr, size = (uint)protocols.Length }, datumPtr, true);

		    int result = Utils.Check(gcm, gnutls_alpn_set_protocols(sess.ptr, datumPtr, 1, AlpnFlagsT.GNUTLS_ALPN_MANDATORY));

			Marshal.FreeHGlobal(valuePtr);
			Marshal.FreeHGlobal(datumPtr);

			return result;
		}
		// int gnutls_alpn_set_protocols (gnutls_session_t session, const gnutls_datum_t * protocols, unsigned protocols_size, unsigned int flags)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_alpn_set_protocols")]
		private static extern int gnutls_alpn_set_protocols(IntPtr session, IntPtr protocols, int protocols_size, AlpnFlagsT flags);

		public static string AlpnGetSelectedProtocol(Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			DatumT data = new DatumT();
			_ = Utils.Check(gcm, gnutls_alpn_get_selected_protocol(sess.ptr, data), (int)EC.en.GNUTLS_E_REQUESTED_DATA_NOT_AVAILABLE);
			return Marshal.PtrToStringAnsi(data.ptr);
		}
		// int gnutls_alpn_get_selected_protocol (gnutls_session_t session, gnutls_datum_t * protocol)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_alpn_get_selected_protocol")]
		private static extern int gnutls_alpn_get_selected_protocol(IntPtr session, [MarshalAs(UnmanagedType.LPStruct)] DatumT data);


		// C r e d e n t i a l s

		// Set

		public static int CredentialsSet(Credentials cred, Session sess) {
			string gcm = Utils.GetCurrentMethod();
			Logging.LogGnuFunc(gcm);

			return Utils.Check(gcm, gnutls_credentials_set(sess.ptr, CredentialsTypeT.GNUTLS_CRD_CERTIFICATE, cred.ptr));
		}
		// int gnutls_credentials_set (gnutls_session_t session, gnutls_credentials_type_t type, void * cred)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_credentials_set")]
		private static extern int gnutls_credentials_set(IntPtr session, CredentialsTypeT type, IntPtr cred);
	}
}
