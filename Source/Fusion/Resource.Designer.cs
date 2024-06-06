﻿//-----------------------------------------------------------------------------
//
//	This code was generated by a ResourceWrapper.Generator Version 3.0.0.0.
//
//	Changes to this file may cause incorrect behavior and will be lost if
//	the code is regenerated.
//
//-----------------------------------------------------------------------------

namespace Fusion {
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using System.ComponentModel;
	using System.Resources;

	/// <summary>
	/// A strongly-typed resource class, for looking up localized strings, etc.
	/// </summary>
	// This class was auto-generated.
	// To add or remove a member, edit your .ResX file then rerun MsBuild,
	// or rebuild your VS project.
	[DebuggerNonUserCodeAttribute()]
	[CompilerGeneratedAttribute()]
	internal static class Resource {

		/// <summary>
		/// Overrides the current thread's CurrentUICulture property for all
		/// resource lookups using this strongly typed resource class.
		/// </summary>
		[EditorBrowsableAttribute(EditorBrowsableState.Advanced)]
		public static CultureInfo Culture { get; set; }

		/// <summary>
		/// Used for formating of the resource strings. Usually same as CultureInfo.CurrentCulture.
		/// </summary>
		[EditorBrowsableAttribute(EditorBrowsableState.Advanced)]
		public static CultureInfo FormatCulture { get; set; }


		private static ResourceManager resourceManager;

		/// <summary>
		/// Returns the cached ResourceManager instance used by this class.
		/// </summary>
		[EditorBrowsableAttribute(EditorBrowsableState.Advanced)]
		public static ResourceManager ResourceManager {
			get {
				if(resourceManager == null) {
					resourceManager = new ResourceManager("Fusion.Resource", typeof(Resource).Assembly);
				}
				return resourceManager;
			}
		}

		/// <summary>
		/// Looks up a localized string similar to Fusion version {0}.
		/// </summary>
 		public static string AppTitle(string version) {
			return string.Format(FormatCulture, ResourceManager.GetString("AppTitle", Culture), version);
		}

		/// <summary>
		/// Looks up a localized string similar to Actual arguments does not match any macro {0} declarations at {1}.
		/// </summary>
 		public static string ArgumentMismatch(string macro, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("ArgumentMismatch", Culture), macro, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Bad format of number: '{0}' at {1}.
		/// </summary>
 		public static string BadNumberFormat(string number, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("BadNumberFormat", Culture), number, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Expected binary format type 8, 16, or 32 instead of {0} at {1}.
		/// </summary>
 		public static string BinaryTypeExpected(string text, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("BinaryTypeExpected", Culture), text, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Binary type already defined at {0}.
		/// </summary>
 		public static string BinaryTypeRedefined(Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("BinaryTypeRedefined", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to File {0} was modified after compilation started.
		/// </summary>
 		public static string FileChanged(string file) {
			return string.Format(FormatCulture, ResourceManager.GetString("FileChanged", Culture), file);
		}

		/// <summary>
		/// Looks up a localized string similar to File not found: {0}.
		/// </summary>
 		public static string FileNotFound(string file) {
			return string.Format(FormatCulture, ResourceManager.GetString("FileNotFound", Culture), file);
		}

		/// <summary>
		/// Looks up a localized string similar to Output folder not found: {0}.
		/// </summary>
 		public static string FolderNotFound(string folder) {
			return string.Format(FormatCulture, ResourceManager.GetString("FolderNotFound", Culture), folder);
		}

		/// <summary>
		/// Looks up a localized string similar to Include file "{0}" not found at {1}.
		/// </summary>
 		public static string IncludeFileNotFound(string file, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("IncludeFileNotFound", Culture), file, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Include folder "{0}" not found.
		/// </summary>
 		public static string IncludeFolderNotFound(string folder) {
			return string.Format(FormatCulture, ResourceManager.GetString("IncludeFolderNotFound", Culture), folder);
		}

		/// <summary>
		/// Looks up a localized string similar to Condition is incomplete value. Only already defined labels can be used in condition:
		/// {0}.
		/// </summary>
 		public static string IncompleteCondition(string position) {
			return string.Format(FormatCulture, ResourceManager.GetString("IncompleteCondition", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Inconclusive error message
		/// {0}.
		/// </summary>
 		public static string IncompleteError(string position) {
			return string.Format(FormatCulture, ResourceManager.GetString("IncompleteError", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to String concatenation is incomplete value. Only already defined labels can be used in string concatenation:
		/// {0}.
		/// </summary>
 		public static string IncompleteString(string position) {
			return string.Format(FormatCulture, ResourceManager.GetString("IncompleteString", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Attempt to write too big number ({0}) to the output at offset {1}. The value replaced with 0x{2:X}.
		/// </summary>
 		public static string IncorrectNumber(int value, long offset, int replaced) {
			return string.Format(FormatCulture, ResourceManager.GetString("IncorrectNumber", Culture), value, offset, replaced);
		}

		/// <summary>
		/// Looks up a localized string similar to Label {0} hides parameter in macro {1} at {2}.
		/// </summary>
 		public static string LabelHidesParameter(string label, string macro, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("LabelHidesParameter", Culture), label, macro, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Label {0} redefined in macro {1} at {2}.
		/// </summary>
 		public static string LabelRedefined(string label, string macro, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("LabelRedefined", Culture), label, macro, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Macro {0} redefined at {1}.
		/// </summary>
 		public static string MacroNameRedefinition(string name, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("MacroNameRedefinition", Culture), name, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Macro "main" is missing..
		/// </summary>
 		public static string MainMissing {
			get { return ResourceManager.GetString("MainMissing", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to Macro main should not have any parameters..
		/// </summary>
 		public static string MainPararameters {
			get { return ResourceManager.GetString("MainPararameters", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to {0}:
		/// {1}.
		/// </summary>
 		public static string MessageOnStack(string message, string position) {
			return string.Format(FormatCulture, ResourceManager.GetString("MessageOnStack", Culture), message, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Single number value expected:
		/// {0}.
		/// </summary>
 		public static string NumberValueExpected(string position) {
			return string.Format(FormatCulture, ResourceManager.GetString("NumberValueExpected", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Name of parameter can not be a keyword "{0}" at {1}.
		/// </summary>
 		public static string ParameterKeyword(string keyword, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("ParameterKeyword", Culture), keyword, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Macro {0} already contains parameter {1} at {2}.
		/// </summary>
 		public static string ParameterRedefinition(string macro, string parameter, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("ParameterRedefinition", Culture), macro, parameter, position);
		}

		/// <summary>
		/// Looks up a localized string similar to {0} ({1}, {2}).
		/// </summary>
 		public static string PositionText(string file, int line, int column) {
			return string.Format(FormatCulture, ResourceManager.GetString("PositionText", Culture), file, line, column);
		}

		/// <summary>
		/// Looks up a localized string similar to Number or string value expected:
		/// {0}.
		/// </summary>
 		public static string StringOrNumberValueExpected(string position) {
			return string.Format(FormatCulture, ResourceManager.GetString("StringOrNumberValueExpected", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to String value expected:
		/// {0}.
		/// </summary>
 		public static string StringValueExpected(string position) {
			return string.Format(FormatCulture, ResourceManager.GetString("StringValueExpected", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to {0} error(s) found.
		/// </summary>
 		public static string SummaryErrors(int errorCount) {
			return string.Format(FormatCulture, ResourceManager.GetString("SummaryErrors", Culture), errorCount);
		}

		/// <summary>
		/// Looks up a localized string similar to Compilation is successful.
		/// </summary>
 		public static string SummarySuccess {
			get { return ResourceManager.GetString("SummarySuccess", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to {0} ({1}, {2}): Syntax error: {3}.
		/// </summary>
 		public static string SyntaxError(string file, int line, int column, string message) {
			return string.Format(FormatCulture, ResourceManager.GetString("SyntaxError", Culture), file, line, column, message);
		}

		/// <summary>
		/// Looks up a localized string similar to Undefined macro {0} at {1}.
		/// </summary>
 		public static string UndefinedMacro(string name, Position poisition) {
			return string.Format(FormatCulture, ResourceManager.GetString("UndefinedMacro", Culture), name, poisition);
		}

		/// <summary>
		/// Looks up a localized string similar to Usage: fusion [options] <InputFileName.asm> <OutputFileName.bin>
		/// available options:.
		/// </summary>
 		public static string Usage {
			get { return ResourceManager.GetString("Usage", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to in macro {0} at {1}.
		/// </summary>
 		public static string UserErrorPosition(string macroName, Position position) {
			return string.Format(FormatCulture, ResourceManager.GetString("UserErrorPosition", Culture), macroName, position);
		}
	}
}
