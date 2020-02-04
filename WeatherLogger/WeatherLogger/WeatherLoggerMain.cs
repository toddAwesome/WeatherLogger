using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Xml;
using System.Threading;
using System.Runtime.InteropServices;
using MemoryQueueNET;
using NamedEvents;
using OutputLogging;
using XmlAppConfiguration;
using System.Xml.Serialization;

namespace WeatherLogger {
    #region WeatherLoggerMain Class
    /// <summary>
    /// Weather Logger Main class
    /// </summary>
    /// <remarks>
    /// The main class for WeatherLogger
    /// </remarks>
    /// <name>WeatherLoggerMain</name>
    /// <filename>WeatherLoggerMain.cs</filename>
    /// <requires>
    /// WeatherLoggerConfiguration Class
    /// WeatherLoggerOperation Class
    /// </requires>
    /// <author>Bryan Bennett</author>
    /// <created>July 13, 2006</created>
    class WeatherLoggerMain {
        /// <summary>
        /// Weather Logger Main function
        /// </summary>
        /// <remarks>
        /// The main entry point for WeatherLogger.
        /// </remarks>
        /// <name>WeatherLoggerMain</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        static void Main(string[] args) {
            WeatherLoggerOperation mOper = new WeatherLoggerOperation();
            try {
                string cfgPath = string.Empty;
                if (args.Length < 1) {
                    cfgPath = Path.ChangeExtension(Environment.CommandLine.Trim("\" ".ToCharArray()),
                                                   ".XML");
                    if (File.Exists(cfgPath) == false) {
                        throw new Exception("A full path to the XML " +
                                            "configuration file was not " +
                                            "specified and " + cfgPath +
                                            " did not exist.");
                    } // if Exists() == false
                } // if Length < 1
                else {
                    foreach (string curArg in args) {
                        cfgPath += (curArg + " ");
                    } // foreach curArg
                    cfgPath = cfgPath.Trim((" ").ToCharArray());
                }
                mOper.Init(cfgPath);
                mOper.Run();
            } // try
            catch (Exception aExp) {
                if (mOper.mLogInitialized == true) {
                    if (mOper.mLog.Print(0, aExp.Message) == false) {
                        Console.WriteLine("Could not print to the OutputLog. " +
                                          "Original message is:");
                        Console.WriteLine(aExp.Message);
                    } // if Print() == false
                } // if mLogInitialized == true
                else {
                    Console.WriteLine(aExp.Message);
                } // else
                Console.WriteLine("Press Any Key To Exit...");
                Console.ReadKey();
            } // catch aExp
            try {
                mOper.Uninit();
            } // try
            catch (Exception aExp) {
                Console.WriteLine(aExp.Message);
                Console.WriteLine("Press Any Key To Exit...");
                Console.ReadKey();
            } // catch
        } // Main()
    } // class WeatherLoggerMain
    #endregion

    #region SystemTimeManaged Class
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SystemTimeManaged                 // Total size 16 bytes
    {
        public UInt16 mYear;
        public UInt16 mMonth;
        public UInt16 mDayOfWeek;
        public UInt16 mDay;
        public UInt16 mHour;
        public UInt16 mMinute;
        public UInt16 mSecond;
        public UInt16 mMilliseconds;

        public SystemTimeManaged() {
            mYear = 0;
            mMonth = 0;
            mDayOfWeek = 0;
            mDay = 0;
            mHour = 0;
            mMinute = 0;
            mSecond = 0;
            mMilliseconds = 0;
        } // SystemTimeManaged()
    } // class SystemTimeManaged
    #endregion

    #region GlobalHeader Class
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GlobalHeader                      // Total size 84 bytes
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string mDataType;         // Null terminated string
                                         // containing NB,OTO, RMS,
                                         // LOFAR, etc.
        public uint mChannelId;        // Numeric channel id
        public uint mSequenceNumber;   // Sequence number for this
                                       // channel
        public uint mTotalHeaderSize;  // Number of bytes in both
                                       // the global and local
                                       // headers
        public uint mDataSize;         // Number of bytes in the
                                       // data array that follows
                                       // the headers
        [MarshalAs(UnmanagedType.U1)]
        public bool mFirstFlag;        // First buffer for this
                                       // channel
        [MarshalAs(UnmanagedType.U1)]
        public bool mLastFlag;         // Last buffer for this
                                       // channel
        [MarshalAs(UnmanagedType.U1)]
        public bool mAbortFlag;        // Indicates abnormal end
                                       // data for channel
        [MarshalAs(UnmanagedType.U1)]
        public bool mValidFlag;        // Does buffer contain valid
                                       // data
        public SystemTimeManaged mStartTime;        // Start time of the data
        public SystemTimeManaged mStopTime;         // Stop time of the data

        public GlobalHeader() {
            mStartTime = new SystemTimeManaged();
            mStopTime = new SystemTimeManaged();
        } // GlobalHeader()
    } // class GlobalHeader
    #endregion

    #region WeatherHeader Class
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WeatherHeader                     // Total size 42 bytes
    {
        public SystemTimeManaged mRecordTime;
        public ushort mWindDirection;
        public Single mWindSpeed;
        public ushort mSolar;
        public Single mInsideTemp;
        public Single mOutsideTemp;
        public ushort mHumidity;
        public Single mPressure;
        public Single mRainFall;

        public WeatherHeader() {
            mRecordTime = new SystemTimeManaged();
            mWindDirection = 0;
            mWindSpeed = 0;
            mSolar = 0;
            mInsideTemp = 0;
            mOutsideTemp = 0;
            mHumidity = 0;
            mPressure = 0;
            mRainFall = 0;
        } // WeatherHeader()
    } // class WeatherHeader
    #endregion

    #region WeatherPacket Class
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WeatherPacket                     // Total size 382 bytes
    {
        public GlobalHeader mGlobalHeader;
        public WeatherHeader mLocalHeader;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string mSerialString;

        public WeatherPacket() {
            mGlobalHeader = new GlobalHeader();
            mLocalHeader = new WeatherHeader();
            mSerialString = string.Empty;
        } // WeatherPacket()
    } // class WeatherPacket
    #endregion

    #region WeatherLoggerOperation Class
    /// <summary>
    /// WeatherLogger operation class
    /// </summary>
    /// <remarks>
    /// This class handles WeatherLogger operations.
    /// </remarks>
    /// <name>WeatherLoggerOperation</name>
    /// <filename>WeatherLoggerMain.cs</filename>
    /// <requires>
    /// OutputLog.cs
    /// MemoryQueue.dll
    /// CSMemoryQueue.dll
    /// NamedEvents.dll
    /// </requires>
    /// <author>Bryan Bennett</author>
    /// <created>July 13, 2006</created>
    public class WeatherLoggerOperation {
        #region Public Variables
        public OutputLog mLog;
        public bool mLogInitialized {
            get { return _mLogInitialized; }
        } // property mLogInitialized
        public bool mInitialized {
            get { return _mInitialized; }
        } // property mInitialized
        #endregion

        #region Private Variables
        private WeatherLoggerConfiguration mConfig;
        private int mOutputStatusTime;
        private MemoryQueue mOutQueue;
        private bool _mLogInitialized = false;
        private bool _mInitialized = false;
        private bool mRunFinished = true;
        private byte[] mQueueBuf;
        private SerialPort mSerialPort;
        private Thread mReadThread;
        private AutoResetEvent mReadEvent;
        private AutoResetEvent mReadTerminate;
        private Mutex mReadMutex;
        private Queue mRecords;
        private uint mCurSeqNum = 0;
        #endregion

        #region WeatherLoggerOperation()
        /// <summary>
        /// Constructor for WeatherLoggerOperation class
        /// </summary>
        /// <remarks>
        /// This function handles construction of the WeatherLoggerOperation class.
        /// </remarks>
        /// <name>WeatherLoggerOperation</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        public WeatherLoggerOperation() {
            mConfig = new WeatherLoggerConfiguration();
            mLog = new OutputLog();
            mOutQueue = null;
        } // WeatherLoggerOperation()
        #endregion

        #region Uninit()
        /// <summary>
        /// Uninitialize WeatherLogger
        /// </summary>
        /// <remarks>
        /// This function unintializes WeatherLogger.
        /// </remarks>
        /// <name>Uninit</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        public void Uninit() {
            if (_mInitialized == false) {
                return;
            } // if _mInitialized == false
              //if (mConfig.mWlConfig.OutputQueue.OpenMethod.ToUpper().IndexOf(
              //    "ATTACH") >= 0)
              //{
              //   if (mOutQueue.DetachQueue() == false)
              //   {
              //      throw new Exception("Could not detach from the OutputQueue.");
              //   } // if DetachQueue() == false
              //} // if IndexOf("ATTACH") >= 0
              //else
              //{
              //   if (mOutQueue.DeleteQueue() == false)
              //   {
              //      throw new Exception("Could not delete the OutputQueue.");
              //   } // if DeleteQueue() == false
              //} // else
            _mInitialized = false;
        } // Uninit()
        #endregion

        #region InitOuputLog()
        /// <summary>
        /// Initialize OutputLog
        /// </summary>
        /// <remarks>
        /// This function intializes the OutputLog component.
        /// </remarks>
        /// <name>InitOutputLog</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        private void InitOutputLog() {
            string logFileModeStr =
                   mConfig.mWlConfig.OutputLogInfo.LogFileMode.ToUpper();
            OutputLog.eLogFileMode logFileMode =
                   OutputLog.eLogFileMode.LOG_MODE_NO_FILE;
            if (logFileModeStr.Equals(string.Empty) == true ||
                logFileModeStr.IndexOf("NONE") >= 0) {
                logFileMode = OutputLog.eLogFileMode.LOG_MODE_NO_FILE;
            } // if logFileModeStr.Equals(Empty) == true || IndexOf("NONE") >= 0
            else if (logFileModeStr.IndexOf("ALL") >= 0) {
                logFileMode = OutputLog.eLogFileMode.LOG_MODE_ALL_TO_FILE;
            } // else if IndexOf("ALL") > = 0
            else if (logFileModeStr.IndexOf("SCREEN") >= 0) {
                logFileMode = OutputLog.eLogFileMode.LOG_MODE_SCREEN_TO_FILE;
            } // else if IndexOf("SCREEN") >= 0
            else {
                throw new XmlException("Could not interpret the LogFileMode in " +
                                       "the XML config file.");
            } // else
            bool overwriteLog = true;
            if (mConfig.mWlConfig.OutputLogInfo.OverwriteLog.ToUpper(
                ).IndexOfAny(("TY1").ToCharArray()) >= 0) {
                overwriteLog = true;
            } // if IndexOfAny("TY1") >= 0
            else if (mConfig.mWlConfig.OutputLogInfo.OverwriteLog.ToUpper(
                     ).IndexOfAny(("FN0").ToCharArray()) >= 0) {
                overwriteLog = false;
            } // else if IndexOfAny("FN0") >= 0
            else {
                throw new XmlException("Could not interpret the OverwriteLog " +
                                       "in the XML config file.");
            } // else
            if (mLog.Init(mConfig.mWlConfig.OutputLogInfo.DiagnosticLevel,
                          mConfig.mWlConfig.ModuleName, logFileMode,
                          mConfig.mWlConfig.OutputLogInfo.LogFileName,
                          overwriteLog) == false) {
                throw new Exception("Could not initialize the output log.");
            } // if Init() == false
            mOutputStatusTime = mConfig.mWlConfig.OutputLogInfo.OutputStatusTime;
            _mLogInitialized = true;
        } // InitOutputLog()
        #endregion

        #region InitQueue()
        /// <summary>
        /// Initialize a Memory Queue
        /// </summary>
        /// <remarks>
        /// This function intializes a Memory Queue component.
        /// </remarks>
        /// <name>InitQueue</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        /// <param name="aQueueCfg">
        /// The Memory Queue configuration
        /// </param>
        /// <param name="aQueue">
        /// The Memory Queue component to initialize
        /// </param>
        private void InitQueue(XmlAppConfig.MemQueueCfg aQueueCfg,
                               ref MemoryQueue aQueue) {
            try {
                aQueue = new MemoryQueue(aQueueCfg.Name, aQueueCfg.NumBuffers,
                                         aQueueCfg.BufferSize);
                if (aQueueCfg.OpenMethod.ToUpper().IndexOf("ATTACH") >= 0) {
                    if (aQueue.OpenedFirst) {
                        throw new Exception("The " + aQueueCfg.Name +
                                  " MemoryQueue has not been created.");
                    } // if OpenedFirst
                } // if OpenMethod == ATTACH
                else if (aQueueCfg.OpenMethod.ToUpper().IndexOf("CREATE") >= 0) {
                    if (!aQueue.OpenedFirst) {
                        throw new Exception("The " + aQueueCfg.Name +
                                  " MemoryQueue has already been created.");
                    } // if !OpenedFirst
                } // else if OpenMethod == CREATE
            } // try
            catch (Exception exp) {
                throw new Exception("Could not " + aQueueCfg.OpenMethod +
                                    " the " + aQueueCfg.Name + " MemoryQueue.",
                                    exp);
            } // catch exp
        } // InitQueue()
        #endregion

        #region InitSerialPort()
        /// <summary>
        /// Initialize the serial port
        /// </summary>
        /// <remarks>
        /// This function intializes the serial port component.
        /// </remarks>
        /// <name>InitSerialPort</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        private void InitSerialPort() {
            Parity parity;
            switch (mConfig.mWlConfig.ComPortInfo.Parity.ToUpper()[0]) {
                case 'E': {
                        parity = Parity.Even;
                        break;
                    } // case E
                case 'M': {
                        parity = Parity.Mark;
                        break;
                    } // case M
                case 'N': {
                        parity = Parity.None;
                        break;
                    } // case N
                case 'O': {
                        parity = Parity.Odd;
                        break;
                    } // case O
                case 'S': {
                        parity = Parity.Space;
                        break;
                    } // case S
                default: {
                        throw new Exception("Could not interpret Parity under " +
                                            "ComPortInfo.");
                    } // default
            } // switch Parity
            StopBits stopBits;
            float tmpStopBits = mConfig.mWlConfig.ComPortInfo.StopBits;
            if (tmpStopBits < 0.5) {
                stopBits = StopBits.None;
            } // if tmpStopBits < 0.5
            else if (tmpStopBits >= 0.5 && tmpStopBits < 1.25) {
                stopBits = StopBits.One;
            } // if tmpStopBits >= 0.5 && < 1.25
            else if (tmpStopBits >= 1.25 && tmpStopBits < 1.75) {
                stopBits = StopBits.OnePointFive;
            } // if tmpStopBits >= 1.25 && < 1.75
            else if (tmpStopBits >= 1.75 && tmpStopBits < 2.5) {
                stopBits = StopBits.Two;
            } // if tmpStopBits >= 1.75 && < 2.5\
            else {
                throw new Exception("Could not interpret StopBits under " +
                                    "ComPortInfo.");
            } // else
            mSerialPort = new SerialPort(mConfig.mWlConfig.ComPortInfo.PortName,
                                         mConfig.mWlConfig.ComPortInfo.BaudRate,
                                         parity,
                                         mConfig.mWlConfig.ComPortInfo.DataBits,
                                         stopBits);
            mRecords = new Queue();
            mReadEvent = new AutoResetEvent(false);
            mReadTerminate = new AutoResetEvent(false);
            mReadMutex = new Mutex(false);
            mReadThread = new Thread(new ThreadStart(ReadThread));
            mReadThread.Start();
        } // InitSerialPort()
        #endregion

        #region Init()
        /// <summary>
        /// Initialize WeatherLogger
        /// </summary>
        /// <remarks>
        /// This function intializes WeatherLogger.
        /// </remarks>
        /// <name>Init</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        /// <param name="aCfgPath">
        /// The full path to the XML configuration file
        /// </param>
        public void Init(string aCfgPath) {
            Console.WriteLine("Reading XML Config");
            mConfig.Read(aCfgPath);
            InitOutputLog();
            mLog.Print(1, "Output Log Initialized");
            mLog.Print(1, "ModuleName = " + mConfig.mWlConfig.ModuleName);
            mLog.Print(1, "ReadyEventName = " +
                       mConfig.mWlConfig.ReadyEventName);
            mLog.Print(1, "OutputLogInfo");
            mLog.Print(1, "   DiagnosticLevel = " +
                       mConfig.mWlConfig.OutputLogInfo.DiagnosticLevel);
            mLog.Print(1, "   LogFileMode = " +
                       mConfig.mWlConfig.OutputLogInfo.LogFileMode);
            mLog.Print(1, "   LogFileName = " +
                       mConfig.mWlConfig.OutputLogInfo.LogFileName);
            mLog.Print(1, "   OverwriteLog = " +
                       mConfig.mWlConfig.OutputLogInfo.OverwriteLog);
            mLog.Print(1, "   OutputStatusTime = " +
                       mConfig.mWlConfig.OutputLogInfo.OutputStatusTime);
            mLog.Print(1, "OutputQueue");
            mLog.Print(1, "   Name = " + mConfig.mWlConfig.OutputQueue.Name);
            mLog.Print(1, "   NumBuffers = " +
                       mConfig.mWlConfig.OutputQueue.NumBuffers);
            mLog.Print(1, "   BufferSize = " +
                       mConfig.mWlConfig.OutputQueue.BufferSize);
            mLog.Print(1, "   OpenMethod = " +
                       mConfig.mWlConfig.OutputQueue.OpenMethod);
            mLog.Print(1, "ComPortInfo");
            mLog.Print(1, "   PortName = " +
                       mConfig.mWlConfig.ComPortInfo.PortName);
            mLog.Print(1, "   BaudRate = " +
                       mConfig.mWlConfig.ComPortInfo.BaudRate);
            mLog.Print(1, "   DataBits = " +
                       mConfig.mWlConfig.ComPortInfo.DataBits);
            mLog.Print(1, "   Parity = " +
                       mConfig.mWlConfig.ComPortInfo.Parity);
            mLog.Print(1, "   StopBits = " +
                       mConfig.mWlConfig.ComPortInfo.StopBits);
            mLog.Print(1, "OutputChannelId = " +
                       mConfig.mWlConfig.OutputChannelId);
            mLog.Print(1, "TimeOffset = " +
                       mConfig.mWlConfig.TimeOffset);
            mLog.Print(1, "DeviceStreamFormat = " +
                       mConfig.mWlConfig.DeviceStreamFormat.ToString());
            InitQueue(mConfig.mWlConfig.OutputQueue, ref mOutQueue);
            mLog.Print(1, "OutputQueue Initialized");
            InitSerialPort();
            mLog.Print(1, "COM Port Initialized");
            Event readyEvent = new Event(mConfig.mWlConfig.ReadyEventName);
            if (readyEvent.mAutoEvent.Set() == false) {
                throw new Exception("Could not set the ready event " +
                                    mConfig.mWlConfig.ReadyEventName);
            } // if Set() == false
            mLog.Print(0, "WeatherLogger Initialized");
            _mInitialized = true;
        } // Init()
        #endregion

        #region Run()
        /// <summary>
        /// Run WeatherLogger
        /// </summary>
        /// <remarks>
        /// This function puts WeatherLogger into full run operation.
        /// </remarks>
        /// <name>Run</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        public void Run() {
            mQueueBuf = new byte[Marshal.SizeOf(new WeatherPacket())];
            DateTime lastStatus = DateTime.Now;
            mLog.Print(0, "Logging Weather Data");
            mRunFinished = false;
            try {
#if !DEBUG
            mSerialPort.Open();
#endif
                mReadEvent.Set();
                while (mRunFinished == false) {
                    if (Console.KeyAvailable == true) {
                        if (Console.ReadKey().Key == ConsoleKey.Escape) {
                            mRunFinished = true;
                        } // if KeyChar == Escape
                    } // if KeyAvailable == true
                    if (mRecords.Count == 0) {
                        Thread.Sleep(500);
                    } // if Count == 0
                    else {
                        while (mRecords.Count > 0) {
                            SendWeatherPacket();
                        } // if Count > 0
                    } // else
                    if (((TimeSpan)(DateTime.Now - lastStatus)).Seconds >
                        mConfig.mWlConfig.OutputLogInfo.OutputStatusTime) {
                        mLog.Print(1, "Packets Processed = " + mCurSeqNum);
                        lastStatus = DateTime.Now;
                    } // if Seconds > OutputStatusTime
                } // while mRunFinished == false
            } // try
            catch (Exception aExp) {
                throw aExp;
            } // catch aExp
            finally {
                mReadTerminate.Set();
                if (mReadMutex.WaitOne(5000, true) == false) {
                    mReadThread.Abort();
                    Thread.Sleep(1000);
                } // if IsAlive == true
                if (mSerialPort.IsOpen == true) {
                    mSerialPort.Close();
                } // if IsOpen == true
                while (mRecords.Count > 0) {
                    SendWeatherPacket();
                } // if Count > 0
            } // finally
        } // Run()
        #endregion

        #region SendWeatherPacket()
        public void SendWeatherPacket() {
            if (mOutQueue == null) {
                return;
            } // if mOutQueue == null
            WeatherPacket curRecord = (WeatherPacket)mRecords.Dequeue();
            mLog.Print(2, curRecord.mSerialString);
            IntPtr outData = Marshal.AllocHGlobal(
                             Marshal.SizeOf(curRecord));
            Marshal.StructureToPtr(curRecord, outData, true);
            Marshal.Copy(outData, mQueueBuf, 0,
                         Marshal.SizeOf(curRecord));
            if (mOutQueue.WriteBuffer(mQueueBuf) == false) {
                throw new Exception("Could not write to the output " +
                                    "queue.");
            } // if WriteQueue() == false
        } // SendWeatherPacket()
        #endregion

        #region ReadThread()
        public void ReadThread() {
            bool startReading = false;
            while (startReading == false) {
                if (mReadEvent.WaitOne(1000, true) == true) {
                    startReading = true;
                } // if WaitOne() == true
                if (mReadTerminate.WaitOne(10, true) == true) {
                    return;
                } // if WaitOne() == true
            } // while startReading == false
            mReadMutex.WaitOne();
            bool endReading = false;
            while (endReading == false) {
#if !DEBUG
            if (mSerialPort.IsOpen == false)
            {
               endReading = true;
               mLog.Print(0, "Serial Port is not open.");
               continue;
            } // if IsOpen == false
#endif
                if (mReadTerminate.WaitOne(10, true) == true) {
                    endReading = true;
                    continue;
                } // if WaitOne() == true
#if DEBUG
                string inputStr = "15/04/23 18:48:51 162 001.9MPH 458.9F 063.8F 047.0F 096% 31.264\"  000.00\"";
                Thread.Sleep(1000);
#else
            string inputStr = mSerialPort.ReadLine();
#endif
                WeatherPacket newRecord = new WeatherPacket();
                newRecord.mGlobalHeader.mDataType = "WEATHER";
                newRecord.mGlobalHeader.mChannelId =
                                        mConfig.mWlConfig.OutputChannelId;
                mCurSeqNum++;
                newRecord.mGlobalHeader.mSequenceNumber = mCurSeqNum;
                newRecord.mGlobalHeader.mTotalHeaderSize = 126;
                newRecord.mGlobalHeader.mDataSize = 256;
                newRecord.mGlobalHeader.mFirstFlag = (mCurSeqNum == 1);
                newRecord.mGlobalHeader.mLastFlag = endReading;
                newRecord.mGlobalHeader.mAbortFlag = false;
                newRecord.mGlobalHeader.mValidFlag = true;
                try {
                    switch (mConfig.mWlConfig.DeviceStreamFormat) {
                        case WeatherLoggerConfiguration.StreamFormat.OldWeatherDevice: {
                                OldWeatherDeviceParser(newRecord, inputStr);
                                break;
                            } // case OldWeatherDevice
                        case WeatherLoggerConfiguration.StreamFormat.WeatherReportLogger: {
                                WeatherReportLoggerParser(newRecord, inputStr);
                                break;
                            } // case WeatherReportLogger
                        case WeatherLoggerConfiguration.StreamFormat.UnknownDevice:
                        default: {
                                newRecord.mGlobalHeader.mValidFlag = false;
                                break;
                            } // default
                    } // switch DeviceStreamFormat
                } // try
                catch (Exception exp) {
                    newRecord.mGlobalHeader.mValidFlag = false;
                    mLog.Print(2, exp.Message);
                } // catch
                if (inputStr.Length > 256) {
                    newRecord.mSerialString = inputStr.Substring(0, 256);
                } // if Length > 256
                else {
                    newRecord.mSerialString = inputStr;
                } // else
                mRecords.Enqueue(newRecord);
            } // while endReading == false
            mReadMutex.ReleaseMutex();
        } // ReadThread()
        #endregion

        #region OldWeatherDeviceParser()
        private void OldWeatherDeviceParser(WeatherPacket newRecord,
                                            string inputString) {
            string info = string.Empty;
            try {
                string[] recordParts =
                         inputString.Trim(" \n\r\t".ToCharArray()).Split(
                         " \n\r\t/:".ToCharArray());
                info += "Parts: " + recordParts.Length;
                DateTime curTime = new DateTime(
                         Convert.ToUInt16(recordParts[0]) + 2000,
                         Convert.ToUInt16(recordParts[1]),
                         Convert.ToUInt16(recordParts[2]),
                         Convert.ToUInt16(recordParts[3]),
                         Convert.ToUInt16(recordParts[4]),
                         Convert.ToUInt16(recordParts[5]), 0);
                curTime = curTime.AddHours(mConfig.mWlConfig.TimeOffset);
                info += ", Time: " + curTime.ToString();
                DateTime sysTime = DateTime.UtcNow;
                newRecord.mLocalHeader.mRecordTime.mYear =
                          (ushort)curTime.Year;
                newRecord.mGlobalHeader.mStartTime.mYear =
                          newRecord.mGlobalHeader.mStopTime.mYear =
                          (ushort)sysTime.Year;
                newRecord.mLocalHeader.mRecordTime.mMonth =
                          (ushort)curTime.Month;
                newRecord.mGlobalHeader.mStartTime.mMonth =
                          newRecord.mGlobalHeader.mStopTime.mMonth =
                          (ushort)sysTime.Month;
                newRecord.mLocalHeader.mRecordTime.mDayOfWeek =
                          (ushort)curTime.DayOfWeek;
                newRecord.mGlobalHeader.mStartTime.mDayOfWeek =
                          newRecord.mGlobalHeader.mStopTime.mDayOfWeek =
                          (ushort)sysTime.DayOfWeek;
                newRecord.mLocalHeader.mRecordTime.mDay =
                          (ushort)curTime.Day;
                newRecord.mGlobalHeader.mStartTime.mDay =
                          newRecord.mGlobalHeader.mStopTime.mDay =
                          (ushort)sysTime.Day;
                newRecord.mLocalHeader.mRecordTime.mHour =
                          (ushort)curTime.Hour;
                newRecord.mGlobalHeader.mStartTime.mHour =
                          newRecord.mGlobalHeader.mStopTime.mHour =
                          (ushort)sysTime.Hour;
                newRecord.mLocalHeader.mRecordTime.mMinute =
                          (ushort)curTime.Minute;
                newRecord.mGlobalHeader.mStartTime.mMinute =
                          newRecord.mGlobalHeader.mStopTime.mMinute =
                          (ushort)sysTime.Minute;
                newRecord.mLocalHeader.mRecordTime.mSecond =
                          (ushort)curTime.Second;
                newRecord.mGlobalHeader.mStartTime.mSecond =
                          newRecord.mGlobalHeader.mStopTime.mSecond =
                          (ushort)sysTime.Second;
                newRecord.mLocalHeader.mRecordTime.mMilliseconds =
                          (ushort)curTime.Millisecond;
                newRecord.mGlobalHeader.mStartTime.mMilliseconds =
                          newRecord.mGlobalHeader.mStopTime.mMilliseconds =
                          (ushort)sysTime.Millisecond;
                info += ", Wind Dir: " + recordParts[6];
                newRecord.mLocalHeader.mWindDirection =
                          Convert.ToUInt16(recordParts[6]);
                info += ", Wind Speed: " + recordParts[7];
                newRecord.mLocalHeader.mWindSpeed =
                          Convert.ToSingle(recordParts[7].Trim(
                                           "KTS".ToCharArray()));
                newRecord.mLocalHeader.mSolar =
                          Convert.ToUInt16(recordParts[9].Trim(
                                           "K".ToCharArray()));
                info += ", Inside Temp: " + recordParts[10];
                newRecord.mLocalHeader.mInsideTemp =
                          Convert.ToSingle(recordParts[10].Trim(
                                           "F".ToCharArray()));
                info += ", Outside Temp: " + recordParts[11];
                newRecord.mLocalHeader.mOutsideTemp =
                          Convert.ToSingle(recordParts[11].Trim(
                                           "F".ToCharArray()));
                info += ", Humidity: " + recordParts[12];
                newRecord.mLocalHeader.mHumidity =
                          Convert.ToUInt16(recordParts[12].Trim(
                                           "%".ToCharArray()));
                info += ", Pressure: " + recordParts[13];
                newRecord.mLocalHeader.mPressure =
                          Convert.ToSingle(recordParts[13].Trim(
                                           "\"".ToCharArray()));
                info += ", Rain Fall: " + recordParts[15];
                newRecord.mLocalHeader.mRainFall =
                          Convert.ToSingle(recordParts[15].Trim(
                                           "\"".ToCharArray()));
                info += ", Success";
            } // try
            catch (Exception exp) {
                info += exp.Message;
                throw new Exception(info);
            } // catch exp
            mLog.Print(2, info);
        } // OldWeatherDeviceParser()
        #endregion

        #region WeatherReportLoggerParser()
        private void WeatherReportLoggerParser(WeatherPacket newRecord,
                                               string inputString) {
            string info = string.Empty;
            try {
                string[] recordParts =
                         inputString.Trim(" \n\r\t".ToCharArray()).Split(
                         " \n\r\t/:".ToCharArray(),
                         StringSplitOptions.RemoveEmptyEntries);
                info += "Parts: " + recordParts.Length;
                DateTime curTime = new DateTime(
                         Convert.ToUInt16(recordParts[0]) + 2000,
                         Convert.ToUInt16(recordParts[1]),
                         Convert.ToUInt16(recordParts[2]),
                         Convert.ToUInt16(recordParts[3]),
                         Convert.ToUInt16(recordParts[4]),
                         Convert.ToUInt16(recordParts[5]),
                         0);
                curTime = curTime.AddHours(mConfig.mWlConfig.TimeOffset);
                info += ", Time: " + curTime.ToString();
                DateTime sysTime = DateTime.UtcNow;
                newRecord.mLocalHeader.mRecordTime.mYear =
                          (ushort)curTime.Year;
                newRecord.mGlobalHeader.mStartTime.mYear =
                          newRecord.mGlobalHeader.mStopTime.mYear =
                          (ushort)sysTime.Year;
                newRecord.mLocalHeader.mRecordTime.mMonth =
                          (ushort)curTime.Month;
                newRecord.mGlobalHeader.mStartTime.mMonth =
                          newRecord.mGlobalHeader.mStopTime.mMonth =
                          (ushort)sysTime.Month;
                newRecord.mLocalHeader.mRecordTime.mDayOfWeek =
                          (ushort)curTime.DayOfWeek;
                newRecord.mGlobalHeader.mStartTime.mDayOfWeek =
                          newRecord.mGlobalHeader.mStopTime.mDayOfWeek =
                          (ushort)sysTime.DayOfWeek;
                newRecord.mLocalHeader.mRecordTime.mDay =
                          (ushort)curTime.Day;
                newRecord.mGlobalHeader.mStartTime.mDay =
                          newRecord.mGlobalHeader.mStopTime.mDay =
                          (ushort)sysTime.Day;
                newRecord.mLocalHeader.mRecordTime.mHour =
                          (ushort)curTime.Hour;
                newRecord.mGlobalHeader.mStartTime.mHour =
                          newRecord.mGlobalHeader.mStopTime.mHour =
                          (ushort)sysTime.Hour;
                newRecord.mLocalHeader.mRecordTime.mMinute =
                          (ushort)curTime.Minute;
                newRecord.mGlobalHeader.mStartTime.mMinute =
                          newRecord.mGlobalHeader.mStopTime.mMinute =
                          (ushort)sysTime.Minute;
                newRecord.mLocalHeader.mRecordTime.mSecond =
                          (ushort)curTime.Second;
                newRecord.mGlobalHeader.mStartTime.mSecond =
                          newRecord.mGlobalHeader.mStopTime.mSecond =
                          (ushort)sysTime.Second;
                newRecord.mLocalHeader.mRecordTime.mMilliseconds =
                          (ushort)curTime.Millisecond;
                newRecord.mGlobalHeader.mStartTime.mMilliseconds =
                          newRecord.mGlobalHeader.mStopTime.mMilliseconds =
                          (ushort)sysTime.Millisecond;
                info += ", Wind Dir: " + recordParts[6];
                newRecord.mLocalHeader.mWindDirection =
                          (ushort)((ConvertCompassToDegrees)Enum.Parse(
                          typeof(ConvertCompassToDegrees), recordParts[6], true));
                info += ", Wind Speed: " + recordParts[7];
                if (recordParts[6].Contains("MPH")) {
                    newRecord.mLocalHeader.mWindSpeed =
                              Convert.ToSingle(recordParts[7].Trim(
                                               "MPH".ToCharArray())) / 1.15F;
                } // if MPH
                else if (recordParts[7].Contains("KTS")) {
                    newRecord.mLocalHeader.mWindSpeed =
                              Convert.ToSingle(recordParts[7].Trim(
                                               "KTS".ToCharArray()));
                } // else if KTS
                newRecord.mLocalHeader.mSolar = 0;
                info += ", Inside Temp: " + recordParts[9];
                newRecord.mLocalHeader.mInsideTemp =
                          Convert.ToSingle(recordParts[9].Trim(
                                           "F".ToCharArray()));
                info += ", Outside Temp: " + recordParts[10];
                newRecord.mLocalHeader.mOutsideTemp =
                          Convert.ToSingle(recordParts[10].Trim(
                                           "F".ToCharArray()));
                info += ", Humidity: " + recordParts[11];
                newRecord.mLocalHeader.mHumidity =
                          Convert.ToUInt16(recordParts[11].Trim(
                                           "%".ToCharArray()));
                info += ", Pressure: " + recordParts[12];
                newRecord.mLocalHeader.mPressure =
                          Convert.ToSingle(recordParts[12].Remove(
                          recordParts[11].Length - 1));
                info += ", Rain Fall: " + recordParts[13];
                newRecord.mLocalHeader.mRainFall =
                          Convert.ToSingle(recordParts[13].Trim(
                                           "\"DMT".ToCharArray()));
                info += ", Success";
            } // try
            catch (Exception exp) {
                info += exp.Message;
                throw new Exception(info);
            } // catch exp
            mLog.Print(2, info);
        } // WeatherReportLoggerParser()
        #endregion

        #region ConvertCompassToDegrees Enumeration
        public enum ConvertCompassToDegrees : ushort {
            N = 0,
            NNE = 23,
            NE = 45,
            ENE = 68,
            E = 90,
            ESE = 113,
            SE = 135,
            SSE = 158,
            S = 180,
            SSW = 203,
            SW = 225,
            WSW = 248,
            W = 270,
            WNW = 293,
            NW = 315,
            NNW = 338
        } // enum ConvertCompassToDegrees
        #endregion
    } // class WeatherLoggerOperation
    #endregion

    #region WeatherLoggerConfiguration Class
    /// <summary>
    /// Configuration for WeatherLogger
    /// </summary>
    /// <remarks>
    /// This class handles the configurations for WeatherLogger.
    /// </remarks>
    /// <name>WeatherLoggerConfiguration</name>
    /// <filename>WeatherLoggerMain.cs</filename>
    /// <requires>
    /// XmlAppConfig.cs
    /// </requires>
    /// <author>Bryan Bennett</author>
    /// <created>July 13, 2006</created>
    public class WeatherLoggerConfiguration : XmlAppConfig {
        #region mWlConfig Property
        /// <summary>
        /// WeatherLogger configuration property
        /// </summary>
        /// <remarks>
        /// This property allows access to the XML configuration for
        /// WeatherLogger.
        /// </remarks>
        /// <name>mWlConfig</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        public WeatherLoggerConfig mWlConfig {
            get { return (WeatherLoggerConfig)mCfgObject; }
        } // property mDfConfig
        #endregion

        #region ComPort Class
        /// <summary>
        /// Configuration class for a COM Port
        /// </summary>
        /// <remarks>
        /// This class defines an XML configuration for a COM Port.
        /// </remarks>
        /// <name>ComPort</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        public class ComPort {
            public string PortName = "COM1";
            public int BaudRate = 19200;
            public int DataBits = 8;
            public string Parity = "None";
            public float StopBits = 1;
        } // class ComPort
        #endregion

        #region StreamFormat Enumeration
        public enum StreamFormat {
            UnknownDevice,
            OldWeatherDevice,
            WeatherReportLogger
        } // enum StreamFormat
        #endregion

        #region WeatherLoggerConfig Class
        /// <summary>
        /// Configuration class for WeatherLogger
        /// </summary>
        /// <remarks>
        /// This class defines an XML configuration for WeatherLogger.
        /// </remarks>
        /// <name>WeatherLoggerConfig</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        public class WeatherLoggerConfig {
            public string ModuleName;
            public string ReadyEventName;
            public OutputLogCfg OutputLogInfo;
            public MemQueueCfg OutputQueue;
            public ComPort ComPortInfo;
            public uint OutputChannelId = 0;
            public double TimeOffset = 0;
            [XmlIgnore]
            public StreamFormat DeviceStreamFormat {
                get {
                    try {
                        return (StreamFormat)Enum.Parse(typeof(StreamFormat),
                               _DeviceStreamFormat, true);
                    } // try
                    catch (Exception) {
                        return StreamFormat.UnknownDevice;
                    } // catch Exception
                } // get
                set {
                    _DeviceStreamFormat = value.ToString();
                } // set
            } // property DeviceStreamFormat
            [XmlElement("DeviceStreamFormat")]
            public string _DeviceStreamFormat = "UnknownDevice";
        } // class WeatherLoggerConfig
        #endregion

        #region WeatherLoggerConfiguration()
        /// <summary>
        /// Constructor for WeatherLoggerConfiguration class
        /// </summary>
        /// <remarks>
        /// This function handles construction of the WeatherLoggerConfiguration
        /// class.
        /// </remarks>
        /// <name>WeatherLoggerConfiguration</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        public WeatherLoggerConfiguration() {
            mCfgObject = null;
        } // WeatherLoggerConfiguration()
        #endregion

        #region Read()
        /// <summary>
        /// Read the configuration
        /// </summary>
        /// <remarks>
        /// This function reads the XML configuration for WeatherLogger.
        /// </remarks>
        /// <name>Read</name>
        /// <filename>WeatherLoggerMain.cs</filename>
        /// <author>Bryan Bennett</author>
        /// <created>July 13, 2006</created>
        /// <param name="aFilePath">
        /// The full path to the XML configuration file
        /// </param>
        public void Read(string aFilePath) {
            Read(aFilePath, typeof(WeatherLoggerConfig));
            if (mWlConfig.ModuleName == null) {
                throw new XmlException("You must define a ModuleName in the " +
                                       "config file.");
            } // if ModuleName == null
            if (mWlConfig.OutputQueue == null) {
                throw new XmlException("You must define an OutputQueue in the " +
                                       "config file.");
            } // if OutputQueue == null
            if (mWlConfig.OutputQueue.Name == null ||
                mWlConfig.OutputQueue.Name.Equals(string.Empty) == true) {
                throw new XmlException("You must define a Name for the " +
                                       "OutputQueue in the config file.");
            } // if Name == null || Name.Equals(Empty) == true
            if (mWlConfig.OutputQueue.NumBuffers == 0) {
                throw new XmlException("You must define a NumBuffers greater " +
                                       "than zero for the OutputQueue in the " +
                                       "config file.");
            } // if NumBuffers == 0
            if (mWlConfig.OutputQueue.BufferSize == 0) {
                throw new XmlException("You must define a BufferSize greater " +
                                       "than zero for the OutputQueue in the " +
                                       "config file.");
            } // if BufferSize == 0
            if (mWlConfig.OutputQueue.OpenMethod == null ||
                mWlConfig.OutputQueue.OpenMethod.Equals(string.Empty) == true) {
                mWlConfig.OutputQueue.OpenMethod = "CREATE";
            } // if OpenMethod == null || OpenMethod.Equals(Empty)
            if (mWlConfig.OutputChannelId == 0) {
                throw new XmlException("You must define an OutputChannelId in " +
                                       "the config file.");
            } // if OutputChannelId == null
        } // Read()
        #endregion
    } // class WeatherLoggerConfiguation
    #endregion
} // namespace WeaterLogger
