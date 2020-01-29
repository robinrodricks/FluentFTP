using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {
	public enum FtpOperator {

		/// <summary>
		/// If the value is exactly equal to X
		/// </summary>
		Equals,
		/// <summary>
		/// If the value is anything except for X
		/// </summary>
		NotEquals,
		/// <summary>
		/// If the value is less than X
		/// </summary>
		LessThan,
		/// <summary>
		/// If the value is less than or equal to X
		/// </summary>
		LessThanOrEquals,
		/// <summary>
		/// If the value is more than X
		/// </summary>
		MoreThan,
		/// <summary>
		/// If the value is more than or equal to X
		/// </summary>
		MoreThanOrEquals,
		/// <summary>
		/// If the value is between the range of X and Y
		/// </summary>
		BetweenRange,
		/// <summary>
		/// If the value is outside the range of X and Y
		/// </summary>
		OutsideRange,

	}
}
