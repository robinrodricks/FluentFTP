using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Sockets;
using FluentFTP.Servers;
#if (CORE || NETFX)
using System.Diagnostics;
#endif
#if NET45
using System.Threading.Tasks;
#endif

#if NET45
namespace FluentFTP {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class NET45Compatibility {

		/// <summary>
		/// This creates a <see cref="System.Threading.Tasks.Task{TResult}"/> that represents a pair of begin and end methods
		/// that conform to the Asynchronous Programming Model pattern.  This extends the maximum amount of arguments from
		///  <see cref="o:System.Threading.TaskFactory.FromAsync"/> to 4 from a 3.  
		/// </summary>
		/// <typeparam name="TArg1">The type of the first argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TArg2">The type of the second argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TArg3">The type of the third argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TArg4">The type of the forth argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="factory">The <see cref="TaskFactory"/> used</param>
		/// <param name="beginMethod">The delegate that begins the asynchronous operation</param>
		/// <param name="endMethod">The delegate that ends the asynchronous operation</param>
		/// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="arg3">The third argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="arg4">The forth argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/> delegate</param>
		/// <returns>The created <see cref="System.Threading.Tasks.Task{TResult}"/> that represents the asynchronous operation</returns>
		/// <exception cref="System.ArgumentNullException">
		/// beginMethod is null
		/// or
		/// endMethod is null
		/// </exception>
		public static Task<TResult> FromAsync<TArg1, TArg2, TArg3, TArg4, TResult>(this TaskFactory factory,
			Func<TArg1, TArg2, TArg3, TArg4, AsyncCallback, object, IAsyncResult> beginMethod,
			Func<IAsyncResult, TResult> endMethod,
			TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, object state) {
			if (beginMethod == null) {
				throw new ArgumentNullException("beginMethod");
			}

			if (endMethod == null) {
				throw new ArgumentNullException("endMethod");
			}

			TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, factory.CreationOptions);
			try {
				AsyncCallback callback = delegate (IAsyncResult asyncResult) { tcs.TrySetResult(endMethod(asyncResult)); };

				beginMethod(arg1, arg2, arg3, arg4, callback, state);
			}
			catch {
				tcs.TrySetResult(default(TResult));
				throw;
			}

			return tcs.Task;
		}

	}
}
#endif