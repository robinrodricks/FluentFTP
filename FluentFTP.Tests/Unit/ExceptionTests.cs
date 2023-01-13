using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class ExceptionTests {

		[Fact]
		public void FtpCommandException_includes_CompletionCode_in_message() {

			Action act = () => throw new FtpCommandException("501", "MyErrorMessage");

			var exception = Assert.Throws<FtpCommandException>(act);
			Assert.Contains("501", exception.ToString());
			Assert.Contains("MyErrorMessage", exception.ToString());
		}
	}
}