﻿using System;
using System.IO;

namespace Danganronpa_Another_Tool
{
    public static class PAK
    {
        // If ToPNG == True then convert the images.
        public static void ExtractPAK(string PAKFileAddress, string DestinationDir, Boolean ToPNG)
        {
            if (Directory.Exists(DestinationDir) == false)
                Directory.CreateDirectory(DestinationDir);

            /* If it's one of these two PAKs, then we have to decompress it.
            These two PAKs are present ONLY in DRAE and are compressed in both PSVITA and PC. */
            if (Path.GetFileName(PAKFileAddress) == "file_img_ehon.pak" || Path.GetFileName(PAKFileAddress) == "file_img_kill.pak")
            {
                uint magicidcomp = 0;

                using (FileStream FilePAK = new FileStream(PAKFileAddress, FileMode.Open, FileAccess.Read))
                using (BinaryReader PAKBinReader = new BinaryReader(FilePAK))
                    magicidcomp = PAKBinReader.ReadUInt32();

                // Checks out if the file.pak is compressed.
                if (magicidcomp == 0xA755AAFC)
                {
                    // false = don't replace the original
                    Common.DecompressFileSpikeCompression(PAKFileAddress, Path.GetDirectoryName(PAKFileAddress), false);

                    // The file.pak on which we will work is now the file.dec generated by Scarlet.
                    PAKFileAddress += ".dec";
                }
            }

            // Start extracting the files contained within the file.pak.
            using (FileStream FilePAK = new FileStream(PAKFileAddress, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader PAKBinReader = new BinaryReader(FilePAK))
                {
                    // Stores the number of files in the PAK.
                    uint AmountOfFiles = PAKBinReader.ReadUInt32();

                    // Start a For to extract all the files in the PAK.
                    for (int i = 0; i < AmountOfFiles; i++)
                    {
                        // In "Offset1" is stored the offset to the file we are going to extract.
                        uint FileToBeExtractedSize = 0, Offset1 = PAKBinReader.ReadUInt32();

                        // Stores in "pos" the current position in order to read later next offset.
                        long pos = FilePAK.Position;

                        // If this is not the last file...
                        if (i != AmountOfFiles - 1)
                        {
                            uint Offset2 = PAKBinReader.ReadUInt32();
                            FileToBeExtractedSize = Offset2 - Offset1;
                        }
                        else // If this IS the last file...
                            FileToBeExtractedSize = (uint)(FilePAK.Length - Offset1);

                        // Move at the beginning of the file to be extracted.
                        FilePAK.Seek(Offset1, SeekOrigin.Begin);

                        // Read the file and stores it in "BodyFile".
                        byte[] BodyFile = new byte[FileToBeExtractedSize];
                        FilePAK.Read(BodyFile, 0, BodyFile.Length);

                        string NewFileExtension = Common.GetMagicID(ref BodyFile);

                        string NewFileAddress = Path.Combine(DestinationDir, Path.GetFileName(DestinationDir) + "-[" + i.ToString("D4") + "]");

                        // After establishing its extension, the file is created and saved.
                        using (FileStream Extract = new FileStream(NewFileAddress + NewFileExtension, FileMode.Create, FileAccess.Write))
                            Extract.Write(BodyFile, 0, BodyFile.Length);

                        // If the user has decided to convert images to ".png", then convert them.
                        if (ToPNG == true && (NewFileExtension == ".tga" || NewFileExtension.Contains(".gxt") || NewFileExtension.Contains(".btx") || NewFileExtension.Contains(".SHTXFS") || NewFileExtension.Contains(".SHTXFF") || NewFileExtension.Contains(".SHTX") || NewFileExtension.Contains(".gim")))
                            Images.ConvertToPNG(NewFileAddress + NewFileExtension, DestinationDir, true); // true = delete the original.

                        // Return into the pointer area and takes care of the next file.
                        FilePAK.Seek(pos, SeekOrigin.Begin);
                    }
                }
            }

            // Delete file.pak.dec since it is no longer needed.
            if (Path.GetExtension(PAKFileAddress) == ".dec")
            {
                File.Delete(PAKFileAddress);
                while (File.Exists(PAKFileAddress)) { }
            }

            // If the folder contains only "files.unknown", then delete the folder.
            if (Directory.Exists(DestinationDir) == true && Directory.GetFiles(DestinationDir, "*.png").Length == 0 && Directory.GetFiles(DestinationDir, "*.cmp").Length == 0 && Directory.GetFiles(DestinationDir, "*.gx3").Length == 0
            && Directory.GetFiles(DestinationDir, "*.btx").Length == 0 && Directory.GetFiles(DestinationDir, "*.llfs").Length == 0 && Directory.GetFiles(DestinationDir, "*.gmo").Length == 0
            && Directory.GetFiles(DestinationDir, "*.gxt").Length == 0 && Directory.GetFiles(DestinationDir, "*.tga").Length == 0 && Directory.GetFiles(DestinationDir, "*.font").Length == 0 && Directory.GetFiles(DestinationDir, "*.gim").Length == 0
            && Directory.GetFiles(DestinationDir, "*.lin").Length == 0 && Directory.GetFiles(DestinationDir, "*.pak").Length == 0)
            {
                Directory.Delete(DestinationDir, true);
                while (Directory.Exists(DestinationDir)) { }

                /* If it's a "sub-pak", rename the pak in ".unknown" because it doesn't contain any known files.
                In the case of Files.PAK containing LINs or PAKs with text, we can't change the file extension ortherwise the program will not be able to extract the text from them. */
                if (PAKFileAddress.Contains("MODE]\\EXTRACTED\\") && !PAKFileAddress.Contains("TEXT PAK TYPE 2") && !PAKFileAddress.Contains("TEXT PAK TYPE 3"))
                {
                    // File.Move is not able to overwrite files, so we have to delete the file.unknown if it already exists.
                    if (File.Exists(Path.ChangeExtension(PAKFileAddress, ".unknown")))
                    {
                        File.Delete(Path.ChangeExtension(PAKFileAddress, ".unknown"));
                        while (File.Exists(Path.ChangeExtension(PAKFileAddress, ".unknown"))) { }
                    }

                    File.Move(PAKFileAddress, Path.ChangeExtension(PAKFileAddress, ".unknown"));
                }
            }
            // Otherwise, if the folder contains some paks, unpack them.
            else if (Directory.GetFiles(DestinationDir, "*.pak").Length != 0)
                foreach (string SinglePAK in Directory.GetFiles(DestinationDir, "*.pak", SearchOption.TopDirectoryOnly))
                {
                    ExtractPAK(SinglePAK, Path.Combine(Path.GetDirectoryName(SinglePAK), Path.GetFileNameWithoutExtension(SinglePAK)), ToPNG);

                    // If it was possible to extract at least one file different from .unknown, delete the original pak.
                    if ((Directory.Exists(Path.Combine(Path.GetDirectoryName(SinglePAK), Path.GetFileNameWithoutExtension(SinglePAK))) == true))
                    {
                        File.Delete(SinglePAK);
                        while (File.Exists(SinglePAK)) { }
                    }
                }
        }

        // RepackSubDirs: true == convert to ".pak" subdirectories too, false = don't repack the subdirs.
        public static void RePackPAK(string PakDirToBeRepacked, string DestinationDir, Boolean RepackSubDirs)
        {
            // Check if there are any subfolders and converts them into ".pak".
            if (RepackSubDirs == true && Directory.GetDirectories(PakDirToBeRepacked, "*", SearchOption.TopDirectoryOnly).Length != 0)
                foreach (string SubDir in Directory.GetDirectories(PakDirToBeRepacked, "*", SearchOption.TopDirectoryOnly))
                    RePackPAK(SubDir, PakDirToBeRepacked, true);

            using (FileStream NEWPAK = new FileStream(Path.Combine(DestinationDir, Path.GetFileNameWithoutExtension(PakDirToBeRepacked) + ".pak"), FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter PAKBinaryWriter = new BinaryWriter(NEWPAK))
                {
                    // Stores the full address of all files included into the folder and orders them alphanumerically.
                    string[] FilesAddress = Directory.GetFiles(PakDirToBeRepacked, "*");
                    Array.Sort(FilesAddress, new DRAT.AlphanumComparatorFast());

                    // Write down the n# of the sentences.
                    PAKBinaryWriter.Write((uint)FilesAddress.Length);

                    //  "SentencesOffset" will contain the offset of each file. 
                    uint[] FilesOffset = new uint[FilesAddress.Length];
                    byte Padding = 0x10; // The padding is "0x10" for every DR. 

                    // Stores the current position so that we can come back later and enter the correct offsets.
                    int pos = (int)NEWPAK.Position;

                    // Fills the pointers area with zeros. At the end of the process the area will be overwritten with the correct data.
                    for (int i = 0; i < FilesOffset.Length; i++)
                        PAKBinaryWriter.Write((uint)0x00);

                    // Padding after the pointers zone.
                    if (NEWPAK.Position % Padding != 0)
                        while (NEWPAK.Position % Padding != 0)
                            PAKBinaryWriter.Write((byte)0x0);

                    // Store the size of the area dedicated to pointers. 
                    FilesOffset[0] = (uint)NEWPAK.Position;

                    for (int i = 0; i < FilesAddress.Length; i++)
                    {
                        // It opens every single file, stores it in "BodyFile" and then inserts it into the new ".pak".
                        using (FileStream TempFile = new FileStream(FilesAddress[i], FileMode.Open, FileAccess.Read))
                        {
                            byte[] BodyFile = new byte[TempFile.Length];
                            TempFile.Read(BodyFile, 0, BodyFile.Length);
                            NEWPAK.Write(BodyFile, 0, BodyFile.Length);
                        }

                        if (i < FilesAddress.Length - 1)
                        {
                            // Inserts the padding after each file, except the last.
                            if (NEWPAK.Position % Padding != 0)
                                while (NEWPAK.Position % Padding != 0)
                                    PAKBinaryWriter.Write((byte)0x0);

                            // No need to memorize the last offset because it would point to the EOF. 
                            FilesOffset[i + 1] = (uint)NEWPAK.Position;
                        }
                    }

                    // Comes back in the area dedicated to the offsets and overwrites all the zeros with the correct offsets.
                    PAKBinaryWriter.Seek(pos, SeekOrigin.Begin);
                    for (int i = 0; i < FilesAddress.Length; i++)
                        PAKBinaryWriter.Write(FilesOffset[i]);
                }
            }
        }
    }
}