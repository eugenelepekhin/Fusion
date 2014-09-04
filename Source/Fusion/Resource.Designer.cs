﻿//-----------------------------------------------------------------------------
//
//	This code was generated by a ResourceWrapper.Generator Version 1.0.0.0.
//
//	Changes to this file may cause incorrect behavior and will be lost if
//	the code is regenerated.
//
//-----------------------------------------------------------------------------

namespace Fusion {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Reflection;
	using System.Resources;
	using System.Runtime.CompilerServices;

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
		/// Looks up a localized string similar to Bad format of number: '{0}' at {1}.
		/// </summary>
 		public static string BadNumberFormat(string number, string position) {
			return string.Format(Culture, ResourceManager.GetString("BadNumberFormat", Culture), number, position);
		}

		/// <summary>
		/// Looks up a localized string similar to "{" expected instead of {0} at {1}.
		/// </summary>
 		public static string BeginExpected(string token, string position) {
			return string.Format(Culture, ResourceManager.GetString("BeginExpected", Culture), token, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Expected binary format type 8, 16, or 32 instead of {0} at {1}.
		/// </summary>
 		public static string BinaryTypeExpected(string text, string position) {
			return string.Format(Culture, ResourceManager.GetString("BinaryTypeExpected", Culture), text, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Binary type already defined at {0}.
		/// </summary>
 		public static string BinaryTypeRedefined(string position) {
			return string.Format(Culture, ResourceManager.GetString("BinaryTypeRedefined", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Compiled file get changed between passes..
		/// </summary>
 		public static string FileChanged {
			get { return ResourceManager.GetString("FileChanged", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to Identifier is expected instead of {0} at {1}.
		/// </summary>
 		public static string IdentifierExpected(string text, string position) {
			return string.Format(Culture, ResourceManager.GetString("IdentifierExpected", Culture), text, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Missing file name for include at {0}.
		/// </summary>
 		public static string IncludeFileMissing(string position) {
			return string.Format(Culture, ResourceManager.GetString("IncludeFileMissing", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Include file "{0}" not found at {1}.
		/// </summary>
 		public static string IncludeFileNotFound(string file, string position) {
			return string.Format(Culture, ResourceManager.GetString("IncludeFileNotFound", Culture), file, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Condition is incomplete value. Only already defined labels can be used in condition at {0}.
		/// </summary>
 		public static string IncompleteCondition(string position) {
			return string.Format(Culture, ResourceManager.GetString("IncompleteCondition", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Inconclusive error message at {0}.
		/// </summary>
 		public static string IncompleteError(string position) {
			return string.Format(Culture, ResourceManager.GetString("IncompleteError", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Attempt to write too big number ({0}) to the output at offset {1}. The value replaced with 0x{2:X}.
		/// </summary>
 		public static string IncorrectNumber(int value, long offset, int replaced) {
			return string.Format(Culture, ResourceManager.GetString("IncorrectNumber", Culture), value, offset, replaced);
		}

		/// <summary>
		/// Looks up a localized string similar to Incorrect value of type {0} is written to the output at offset {1}. The value replaced with 0xFF.
		/// </summary>
 		public static string IncorrectValue(string type, long offset) {
			return string.Format(Culture, ResourceManager.GetString("IncorrectValue", Culture), type, offset);
		}

		/// <summary>
		/// Looks up a localized string similar to Internal assembler error..
		/// </summary>
 		public static string InternalError {
			get { return ResourceManager.GetString("InternalError", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to {0} is expected instead of {1} at {2}.
		/// </summary>
 		public static string ItemExpected(string expected, string actual, string position) {
			return string.Format(Culture, ResourceManager.GetString("ItemExpected", Culture), expected, actual, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Label {0} redefined in macro {1} at {2}.
		/// </summary>
 		public static string LabelRedefined(string label, string macro, string position) {
			return string.Format(Culture, ResourceManager.GetString("LabelRedefined", Culture), label, macro, position);
		}

		/// <summary>
		/// Looks up a localized string similar to macro, include, or binary expected at {0}.
		/// </summary>
 		public static string MacroExpected(string position) {
			return string.Format(Culture, ResourceManager.GetString("MacroExpected", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Macro name expected instead of {0} at {1}.
		/// </summary>
 		public static string MacroNameExpected(string text, string position) {
			return string.Format(Culture, ResourceManager.GetString("MacroNameExpected", Culture), text, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Macro {0} redefined at {1}.
		/// </summary>
 		public static string MacroNameRedefinition(string name, string position) {
			return string.Format(Culture, ResourceManager.GetString("MacroNameRedefinition", Culture), name, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Macro "main" is missing..
		/// </summary>
 		public static string MainMissing {
			get { return ResourceManager.GetString("MainMissing", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to Macro main should not has any parameters..
		/// </summary>
 		public static string MainPararameters {
			get { return ResourceManager.GetString("MainPararameters", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to File included more then once: "{0}".
		/// </summary>
 		public static string MultipleInclusions(string path) {
			return string.Format(Culture, ResourceManager.GetString("MultipleInclusions", Culture), path);
		}

		/// <summary>
		/// Looks up a localized string similar to Single number value expected at {0}.
		/// </summary>
 		public static string NumberValueExpected(string position) {
			return string.Format(Culture, ResourceManager.GetString("NumberValueExpected", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Name of parameter can not be a keyword {0} at {1}.
		/// </summary>
 		public static string ParameterKeyword(string keyword, string position) {
			return string.Format(Culture, ResourceManager.GetString("ParameterKeyword", Culture), keyword, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Macro {0} already contains parameter {1} at {2}.
		/// </summary>
 		public static string ParameterRedefinition(string macro, string parameter, string position) {
			return string.Format(Culture, ResourceManager.GetString("ParameterRedefinition", Culture), macro, parameter, position);
		}

		/// <summary>
		/// Looks up a localized string similar to {0} ({1}).
		/// </summary>
 		public static string PositionText(string file, int line) {
			return string.Format(Culture, ResourceManager.GetString("PositionText", Culture), file, line);
		}

		/// <summary>
		/// Looks up a localized string similar to Identifier, literal, "(", or keyword.
		/// </summary>
 		public static string PrimaryItem {
			get { return ResourceManager.GetString("PrimaryItem", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to Number or string value expected at {0}.
		/// </summary>
 		public static string StringOrNumberValueExpected(string position) {
			return string.Format(Culture, ResourceManager.GetString("StringOrNumberValueExpected", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to String value expected at {0}.
		/// </summary>
 		public static string StringValueExpected(string position) {
			return string.Format(Culture, ResourceManager.GetString("StringValueExpected", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to {0} error(s) found.
		/// </summary>
 		public static string SummaryErrors(int errorCount) {
			return string.Format(Culture, ResourceManager.GetString("SummaryErrors", Culture), errorCount);
		}

		/// <summary>
		/// Looks up a localized string similar to Compilation is successful.
		/// </summary>
 		public static string SummarySuccess {
			get { return ResourceManager.GetString("SummarySuccess", Culture); }
		}

		/// <summary>
		/// Looks up a localized string similar to Undefined macro {0} at {1}.
		/// </summary>
 		public static string UndefinedMacro(string name, string poisition) {
			return string.Format(Culture, ResourceManager.GetString("UndefinedMacro", Culture), name, poisition);
		}

		/// <summary>
		/// Looks up a localized string similar to Unexpected char '{0}' (0x{1:X}) in {2}.
		/// </summary>
 		public static string UnexpectedChar(char c, int value, string position) {
			return string.Format(Culture, ResourceManager.GetString("UnexpectedChar", Culture), c, value, position);
		}

		/// <summary>
		/// Looks up a localized string similar to Unexpected end of file at {0}.
		/// </summary>
 		public static string UnexpectedEOF(string position) {
			return string.Format(Culture, ResourceManager.GetString("UnexpectedEOF", Culture), position);
		}

		/// <summary>
		/// Looks up a localized string similar to Usage: fusion <InputFileName.asm> <OutputFileName.bin>.
		/// </summary>
 		public static string Usage {
			get { return ResourceManager.GetString("Usage", Culture); }
		}
	}
}
