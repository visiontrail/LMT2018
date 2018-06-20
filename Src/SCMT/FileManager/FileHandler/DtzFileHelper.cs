﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.FileHandler
{
	public class DtzFileHelper
	{
		[DllImport("LmtBZipUtil.dll", EntryPoint = "UnpackZipPackageSplitForDTFile", CallingConvention = CallingConvention.Cdecl)]
		public static extern long UnpackZipPackageSplitForDTFile(string lpcstrSrcPath, string lpcstrDestPath, ref int subFileNum);

		[DllImport("LmtBZipUtil.dll", EntryPoint = "Aom_Zip_GetFileHead_OupPut", CallingConvention = CallingConvention.Cdecl)]
		public static extern long Aom_Zip_GetFileHead_OupPut(string s8InputFileName, ref CompressFileHead pcompressfilehead);

		[DllImport("LmtBZipUtil.dll", EntryPoint = "UnpackZipPackageSimple", CallingConvention = CallingConvention.Cdecl)]
		public static extern Int32 UnpackZipPackageSimple(string lpcstrSrcPath, string lpcstrDestPath, StringBuilder lpstrFilePath);
	}
}