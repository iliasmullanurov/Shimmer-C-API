﻿using System;
using System.Globalization;
using System.IO;
using System.Text;
using static ShimmerBLEAPI.Devices.VerisenseDevice;

namespace shimmer.Models
{
    public class ProdConfigPayload : BasePayload
    {
        // ASM Props
        public string ConfigHeader { get; set; }
        public string ASMID { get; set; }
        public HardwareIdentifier HardwareIdentifier { get; set; }
        public int REV_HW_MAJOR { get; set; }
        public int REV_HW_MINOR { get; set; }
        public int REV_FW_MAJOR { get; set; }
        public int REV_FW_MINOR { get; set; }

        public int REV_FW_INTERNAL { get; set; }
        public int REV_HW_INTERNAL { get; set; }

        public string PasskeyID { get; set; }
        public string Passkey { get; set; }
        public string AdvertisingNamePrefix { get; set; }

        public ProdConfigPayload(string payload)
        {
            Payload = payload;
            ProcessPayload(GetPayload());
        }

        public ProdConfigPayload()
        {

        }

        public void EnableClinicalTrialPasskey()
        {
            try
            {
                SetAdvertisingNamePrefix("");
                SetPasskey("");
                SetPasskeyID("");
            } catch (Exception ex)
            {
                throw ex;
            }
        }

        public void EnableNoPasskey(string advertisingName, string passkeyID)
        {
            try
            {   
                if (advertisingName.Equals("Verisense"))
                {
                    advertisingName = "";
                }
                SetAdvertisingNamePrefix(advertisingName);
                SetPasskey("");
                SetPasskeyID(passkeyID);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void EnableDefaultPasskey(string advertisingName, string passkeyID)
        {
            try
            {
                if (advertisingName.Equals("Verisense"))
                {
                    advertisingName = "";
                }
                SetAdvertisingNamePrefix(advertisingName);
                SetPasskey("123456");
                SetPasskeyID(passkeyID);
            } catch (Exception ex)
            {
                throw ex;
            }
}

        protected void SetPasskeyID(string passkeyId)
        {
            byte[] payloadArray = GetPayload();
            if (string.IsNullOrEmpty(passkeyId))
            {
                for (int i = 18; i <= 19; i++)
                {
                    payloadArray[i] = 0xFF;
                }
            }
            else if (passkeyId.Length == 2)
            {
                byte[] passkeyIdArray = Encoding.UTF8.GetBytes(passkeyId);
                if (HasAnFF(passkeyIdArray))
                {
                    throw new Exception("Passkey ID has a byte value of 0xFF which is not permitted");
                }
                for (int i = 0; i < passkeyIdArray.Length; i++)
                {
                    payloadArray[i + 18] = passkeyIdArray[i];
                }
            }
            else
            {
                throw new Exception("Passkey ID should have exactly two characters");
            }
            Payload = BitConverter.ToString(payloadArray);
        }

        protected void SetPasskey(string passkey)
        {
            byte[] payloadArray = GetPayload();
            if (string.IsNullOrEmpty(passkey))
            {
                for (int i = 20; i <= 25; i++)
                {
                    payloadArray[i] = 0xFF;
                }
            }
            else if (passkey.Length == 6)
            {
                if (!int.TryParse(passkey, out _))
                {
                    throw new Exception("Passkey Must Be Numeric Values");
                }
                
                byte[] passkeyArray = Encoding.UTF8.GetBytes(passkey);
                for (int i = 0; i < passkeyArray.Length; i++)
                {
                    payloadArray[i + 20] = passkeyArray[i];
                }
            }
            else
            {
                throw new Exception("Passkey should have exactly six characters");
            }
            Payload = BitConverter.ToString(payloadArray);

        }
        
        protected void SetAdvertisingNamePrefix(string advertisingNamePrefix)
        {
            byte[] payloadArray = GetPayload();
            if (string.IsNullOrEmpty(advertisingNamePrefix))
            {
                for (int i = 26; i <= 57; i++)
                {
                    payloadArray[i] = 0xFF;
                }
            }
            else if (advertisingNamePrefix.Length <= 32)
            {
                byte[] advertisingNamePrefixByteArray = Encoding.UTF8.GetBytes(advertisingNamePrefix);
                if (HasAnFF(advertisingNamePrefixByteArray))
                {
                    throw new Exception("Advertising name has a byte value of 0xFF which is not permitted");
                }
                for (int i = 0; i < advertisingNamePrefixByteArray.Length; i++)
                {
                    payloadArray[i + 26] = advertisingNamePrefixByteArray[i];
                }
                for (int i = advertisingNamePrefixByteArray.Length + 26; i <= 57; i++)
                {
                    //set the remaining bytes to 0xFF
                    payloadArray[i] = 0xFF;
                }
            }
            else
            {
                throw new Exception("Advertising name prefix cannot have more than 32 characters");
            }
            Payload = BitConverter.ToString(payloadArray);
        }

        protected bool IsAllFFs(byte[] byteArray)
        {
            foreach (byte b in byteArray)
            {
                if (b != 0xFF)
                {
                    return false;
                }
            }
            return true;
        }

        protected bool HasAnFF(byte[] byteArray)
        {
            foreach (byte b in byteArray)
            {
                if (b == 0xFF)
                {
                    return true;
                }
            }
            return false;
        }

        public new bool ProcessPayload(byte[] response)
        {
            try
            {
                Payload = BitConverter.ToString(response);

                var stream = new MemoryStream(response);

                var reader = new BinaryReader(stream);

                Header = BitConverter.ToString(reader.ReadBytes(1));

                var lenthBytes = reader.ReadBytes(2);
                Array.Reverse(lenthBytes);
                Length = BitConverter.ToInt16(lenthBytes, 0);

                ConfigHeader = BitConverter.ToString(reader.ReadBytes(1));

                var idBytes = reader.ReadBytes(6);
                Array.Reverse(idBytes);
                ASMID = BitConverter.ToString(idBytes).Replace("-", string.Empty);

                REV_HW_MAJOR = int.Parse(BitConverter.ToString(reader.ReadBytes(1)), NumberStyles.HexNumber);
                
                HardwareIdentifier = (HardwareIdentifier)REV_HW_MAJOR;

                REV_HW_MINOR = int.Parse(BitConverter.ToString(reader.ReadBytes(1)), NumberStyles.HexNumber);

                REV_FW_MAJOR = int.Parse(BitConverter.ToString(reader.ReadBytes(1)), NumberStyles.HexNumber);

                REV_FW_MINOR = int.Parse(BitConverter.ToString(reader.ReadBytes(1)), NumberStyles.HexNumber);

                byte[] fwInternalArray = reader.ReadBytes(2);
                REV_FW_INTERNAL = BitConverter.ToUInt16(fwInternalArray, 0);

                if (Length >= 14)
                {
                    byte[] hwInternalArray = reader.ReadBytes(2);
                    REV_HW_INTERNAL = BitConverter.ToUInt16(hwInternalArray, 0);

                    byte[] passkeyIdArray = reader.ReadBytes(2);
                    if (IsAllFFs(passkeyIdArray))
                    {
                        PasskeyID = "";
                    }
                    else
                    {
                        PasskeyID = Encoding.UTF8.GetString(passkeyIdArray);
                    }

                    byte[] passkeyArray = reader.ReadBytes(6);
                    if (IsAllFFs(passkeyArray) || passkeyArray[0] == 0x00)
                    {
                        Passkey = "";
                    }
                    else
                    {
                        Passkey = Encoding.UTF8.GetString(passkeyArray);
                    }

                    byte[] advertisingNamePrefixArrayOriginal = reader.ReadBytes(32);
                    if (IsAllFFs(advertisingNamePrefixArrayOriginal))
                    {
                        AdvertisingNamePrefix = "Verisense";
                    }
                    else
                    {
                        int endIndex = Array.IndexOf(advertisingNamePrefixArrayOriginal, (byte)0xFF);
                        if (endIndex == -1)
                        {
                            endIndex = 32;
                        }
                        byte[] advertisingNamePrefixArray = new byte[endIndex];
                        Array.Copy(advertisingNamePrefixArrayOriginal, 0, advertisingNamePrefixArray, 0, endIndex);
                        AdvertisingNamePrefix = Encoding.UTF8.GetString(advertisingNamePrefixArray);
                    }
                }

                reader.Close();
                stream = null;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
