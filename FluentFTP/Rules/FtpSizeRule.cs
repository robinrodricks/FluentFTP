using System;
using System.Collections.Generic;
using System.Text;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept files that are of the given size, or within the given range of sizes.
	/// </summary>
	public class FtpSizeRule : FtpRule {

		/// <summary>
		/// Which operator to use
		/// </summary>
		public FtpOperator Operator;

		/// <summary>
		/// The first value, required for all operators
		/// </summary>
		public long X;

		/// <summary>
		/// The second value, only required for BetweenRange and OutsideRange operators
		/// </summary>
		public long Y;

		/// <summary>
		/// Only accept files that are of the given size, or within the given range of sizes.
		/// </summary>
		/// <param name="ruleOperator">Which operator to use</param>
		/// <param name="x">The first value, required for all operators</param>
		/// <param name="y">The second value, only required for BetweenRange and OutsideRange operators.</param>
		public FtpSizeRule(FtpOperator ruleOperator, long x, long y = 0) {
			this.Operator = ruleOperator;
			this.X = x;
			this.Y = y;
		}

		/// <summary>
		/// Checks if the file is of the given size, or within the given range of sizes.
		/// </summary>
		public override bool IsAllowed(FtpListItem result) {
			if (result.Type == FtpObjectType.File) {
				return Operators.Validate(Operator, result.Size, X, Y);
			}
			else {
				return true;
			}
		}

	}
}