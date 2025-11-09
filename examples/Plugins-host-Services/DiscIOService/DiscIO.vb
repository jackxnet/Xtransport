Imports xnethost
Imports xPxP
Imports System.Threading.Thread
Imports xPxP.ENUMS
Imports System.Xml
Imports xPxP.xNetPacket
Imports Emgu.CV
Imports Emgu.CV.Structure
Imports Emgu.CV.CvEnum
Imports xnethost.serversocks
Imports xnethost.GDictionaries
Imports Windows.Win32.System
Imports System.IO
Imports System.Net
Imports System.Reflection.Metadata
Imports Emgu.CV.Features2D
Public Class FileService
    Implements IServicePlugin
    Public canceltoken As Boolean
    Private msgr As messengerService

    Public Sub addhostservice(params) Implements IServicePlugin.addhostservice
        Dim hs As New HostService
        Dim p = Split(params, ",")
        hs.id = p(10)
        hs.DoProcess()
    End Sub

    Public Sub drophostservice(params) Implements IServicePlugin.drophostservice

    End Sub


    Public Sub addclientservice(params) Implements IServicePlugin.addclientservice

    End Sub

    Public Sub dropclientservice(params) Implements IServicePlugin.dropclientservice

    End Sub



    Public Sub addservice(params) Implements IServicePlugin.addservice

    End Sub

    Public Sub dropservice(params) Implements IServicePlugin.dropservice
    End Sub

    Public Sub DoProcess() Implements IServicePlugin.DoProcess
        Console.WriteLine("Plugin FileService Running")
        Task.Run(Sub() dataready_node())
    End Sub



    Private Sub dataready_node()
        While canceltoken = False
            While data_ready_node.Count > 0
                Dim rf As return_frame = Nothing
                Dim r = data_ready_node.TryDequeue(rf)
                If r = True Then
                    If rf.id = msgr.id Then
                        msgr.processdata(rf.b)
                    End If

                    'do work with new data
                End If
            End While
            Sleep(10)
        End While
    End Sub


End Class

Public Class messengerService
    Public id As Integer
    Public canceltoken As Boolean
    Public nc As nodeclient
    Public xc As New XClient

    Public Sub buildxclient(params)
        Dim sp = Split(params, ",")
        xc.clienttype = DirectCast([Enum].Parse(GetType(SERVICE_TYPE), sp(0)), SERVICE_TYPE)
        xc.hostid = sp(1)
        xc.hostip = sp(2)
        xc.hostport = sp(3)
        xc.channel = sp(4)
        xc.netlatency = sp(5)
        xc.packetburst = sp(6)
        xc.localid = sp(7)
        xc.localname = sp(8)
        xc.useipv6 = sp(9)
        xc.broadcast = sp(10)
        id = sp(7)
    End Sub


    Public Sub startservice()
        nc = New nodeclient
        nc.config(xc)
        nc.startclient(xc.hostip)
    End Sub



    Public Sub processdata(b)
        Dim xf As New xnetmessage_frame
        Dim xnet As New xNetPacket
        xf = xnet.xDecodemessage(b)

        If xf.controlcodes = CONTROL_TYPE.REQUEST Then

            If xf.messagetype = MESSAGE_TYPE.HOSTINFO Then

                Dim params = xnet.hostinfotoparams(hostinfo)

            End If

        End If



    End Sub

End Class

Public Class OtherService


End Class


Public Class HostService
    Public id As Integer
    Public canceltoken As Boolean
    Private IOp As New DiskIO

    Public Sub DoProcess()
        Task.Run(Sub() dataready_host())
    End Sub

    Private Sub dataready_host()
        While canceltoken = False
            Try
                While data_ready_host.Count > 0
                    Dim b As Byte() = Nothing
                    Dim r = data_ready_host.TryDequeue(b)
                    If r = True Then
                        'Console.WriteLine($"Receiving File  size of file {b.Length}")
                        processdata(b)
                    End If
                End While
                Sleep(10)
            Catch ex As Exception
                Console.WriteLine($"datareadyhost Broke {ex.Message}")
            End Try

        End While
    End Sub




    Private Sub recxpayload(b As Byte())
        Dim xnet As New xNetPacket
        Dim xf As New xnetpayload_frame
        xf = xnet.xDecodepayload(b)
        If xf.payloadtype = PAYLOAD_TYPE.FILE Then
            'Dim filename = Filepaths.filepath & xf.paynorm
            Dim subpath = Split(xf.paynorm, "/")
            If Not IO.Directory.Exists(Filepaths.filepath & subpath(0)) Then
                IO.Directory.CreateDirectory(Filepaths.filepath & subpath(0))
            End If
            Dim filename = Filepaths.filepath & xf.paynorm
            Console.WriteLine($"Receiving File {filename} size of file {xf.payload.Length}")
            'Exit Sub
            ' Task.Run(Sub()

            Dim Fs = New FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)
            Fs.Write(xf.payload) ' // just feed it the contents verbatim
                Fs.Flush()
                Fs.Close()
                xf.payload = Nothing
                Fs.Dispose()
                ' GC.Collect()
                '           End Sub)
            End If

        ' xf.Dispose()
        'xnet.Dispose()


    End Sub



    Private Sub returnsize(requestid As Integer, callhost As Integer, callid As Integer, l As List(Of Integer))
        Console.WriteLine($"Sending Sector used {l(0)}  of size {l(1)}")
        Dim x As New xNetPacket
        Dim xframe As Byte() = x.xEncodeDiskIO(ROUTE_CODE.UNICAST, callhost, callid, hostinfo.channel, hostinfo.hostid, id, MESSAGE_TYPE.DISCIO, CONTROL_TYPE.DISKIO_RETURNDISCSPACE, requestid, l(0), l(1), 0, 0, "SendFile", PAYLOAD_TYPE.FILE, 0, "Buffer", {0})
        serversock.routeunicast(callid, xframe)
    End Sub

    Private Sub returnbuffer(requestid As Integer, callhost As Integer, callid As Integer, sector As Integer, offset As Integer, len As Integer, payload As Byte())

        Console.WriteLine($"Sending Sector {sector} {offset} {len}")
        Dim x As New xNetPacket
        Dim xframe As Byte() = x.xEncodeDiskIO(ROUTE_CODE.UNICAST, callhost, callid, hostinfo.channel, hostinfo.hostid, id, MESSAGE_TYPE.DISCIO, CONTROL_TYPE.DISKIO_RETURNBUFFER, requestid, sector, offset, len, 0, "SendFile", PAYLOAD_TYPE.FILE, 0, "Buffer", payload)
        serversock.routeunicast(callid, xframe)
    End Sub

    Private Sub returnsectorid(requestid As Integer, callhost As Integer, callid As Integer, sector As Integer, offset As Integer, len As Integer)

        Console.WriteLine($"Sending Sector {sector} {offset} {len}")
        Dim x As New xNetPacket
        Dim xframe As Byte() = x.xEncodeDiskIO(ROUTE_CODE.UNICAST, callhost, callid, hostinfo.channel, hostinfo.hostid, id, MESSAGE_TYPE.DISCIO, CONTROL_TYPE.DISKIO_RETURNSECTORID, requestid, sector, offset, len, 0, "SendFile", PAYLOAD_TYPE.FILE, 0, "Buffer", {0})
        serversock.routeunicast(callid, xframe)
    End Sub


    Public Sub processdata(b)
        Dim xnet As New xNetPacket
        Dim xf = New xnetDiskIO_frame
        xf = xnet.xDecodeDiskIO(b)
        processDiskIO(xf)

    End Sub


    Private Sub processDiskIO(x As xnetDiskIO_frame)
        Dim callerid = x.callerid
        Dim callerhost = x.callerhost
        Dim requestid = x.param1
        Dim Sector = x.param2
        Dim offset = x.param3
        Dim len = x.param4
        Dim size = x.param5

        Select Case x.controlcodes

            Case CONTROL_TYPE.DISKIO_CREATESECTOR
                IOp.CreateSector(Sector, size)
            Case CONTROL_TYPE.DISKIO_WRITEFILE
                IOp.WriteFile(Sector, offset, len, x.payload)
                returnsectorid(requestid, callerhost, callerid, Sector, offset, len)
            Case CONTROL_TYPE.DISKIO_READFILE
                IOp.ReadSector(Sector, offset, len, x.payload)
                returnbuffer(requestid, callerhost, callerid, Sector, offset, len, x.payload)
            Case CONTROL_TYPE.DISKIO_DELETEFILE
                IOp.DeleteFile(Sector, offset, len)
            Case CONTROL_TYPE.DISKIO_REQUESTDISCSPACE
                Dim l = IOp.getdiskfree()
                returnsize(requestid, callerhost, callerid, l)


        End Select

    End Sub

End Class


Public Class ClientService
    Public id As Integer
    Public Sub StartService()

    End Sub

End Class

Public Class DiskIO

    Private LPaths As New Concurrent.ConcurrentDictionary(Of String, Path)
    Private LAtributes As New Concurrent.ConcurrentDictionary(Of Integer, Atribute)
    Public blocksize As Integer = 8000
    Public nextkey As Integer = 1

    Public Enum IOStatus
        Success = 0
        Failed = 5
        FileNotFound = 10
    End Enum

    Private Function convertobase65(bits) As String
        Dim bitbytes(bits.Length / 8 - 1) As Byte
        'Dim bitbytes As New Byte()
        bits.CopyTo(bitbytes, 0)
        Return Convert.ToBase64String(bitbytes)
    End Function
    Private Function converfrombase65(st) As BitArray
        Dim newbytes = Convert.FromBase64String(st)
        Dim newbits = New BitArray(newbytes)
        Return newbits
    End Function


    Public Function addsector(sector As Integer)
        Dim Sec(800000000) As Byte
        Dim bits As New BitArray(10000)
        Dim bitbytes((bits.Length - 1) / 8 + 1) As Byte


        Using outFile As New FileStream(Filepaths.filepath & $"Sector{sector}.bin", FileMode.CreateNew, FileAccess.Write)
            outFile.Write(Sec)
            outFile.Flush()
        End Using

        Using outFile As New FileStream(Filepaths.filepath & $"Sector{sector}.map", FileMode.CreateNew, FileAccess.Write)
            outFile.Write(bitbytes)
            outFile.Flush()
        End Using

    End Function



    Private Function FindBlocks(size As Integer, map As BitArray) As Integer

        Dim bitsize = Math.Floor(size / blocksize)
        Dim lastused = 0
        For t = 0 To map.Length - 1
            Dim IsFree = True

            If bitsize > (map.Length - 1) - t Then
                Return -1
            End If

            For z = t To (t + bitsize)
                If map(z) = True Then
                    IsFree = False
                    lastused = z
                End If
            Next

            If IsFree = True Then
                Console.WriteLine($"Found Freespace at {t} to {t + bitsize}")
                Return t
            Else
                t = lastused
            End If

        Next
        Return -1
    End Function

    Private Function FindFreeSpace(ByRef Sector As Integer, ByRef block As Integer, Size As Integer)

        Dim fp = Directory.GetFiles(Filepaths.filepath)
        For Each f In fp
            Dim l = Split(f, "\")
            Dim s = l(l.Count - 1)
            Dim ln = s.Length - 4
            Sector = Val(s.Substring(6, ln - 6))
            Dim map = readmap(Sector)
            block = FindBlocks(Size, map)
            If block > -1 Then Return IOStatus.Success
        Next

        Return IOStatus.Failed
    End Function

    Public Function CreateSector(sector As Integer, size As Integer)
        Dim newb(size) As Byte
        Dim bmap(size / blocksize / 8 - 1) As Byte

        Using outFile As New FileStream(Filepaths.filepath & $"Sector{sector}.bin", FileMode.CreateNew, FileAccess.Write)
            outFile.Write(newb)
            outFile.Flush()
        End Using

        Using outFile As New FileStream(Filepaths.filepath & $"Sector{sector}.map", FileMode.CreateNew, FileAccess.Write)
            outFile.Write(bmap)
            outFile.Flush()
        End Using

        Return IOStatus.Success
    End Function


    Public Function ReadSector(sector As Integer, offset As Integer, len As Integer, ByRef buffer As Byte())
        Using inFile As New FileStream(Filepaths.filepath & $"Sector{sector}.bin", FileMode.Open, FileAccess.Read)
            ReDim buffer(len - 1)
            Dim bytesRead As Integer
            inFile.Seek(offset, 0)
            bytesRead = inFile.Read(buffer, 0, len)
            Console.WriteLine($"Read Sector {sector} offset {offset} Length{len} bytes.")
        End Using
        Return IOStatus.Success
    End Function

    Public Function WriteSector(ByRef sector As Integer, ByRef buffer As Byte(), ByRef offset As Integer, ByRef len As Integer)
        Using outFile As New FileStream(Filepaths.filepath & $"Sector{sector}.bin", FileMode.Open, FileAccess.Write)
            outFile.Seek(offset, 0)
            outFile.Write(buffer, 0, len)
            Console.WriteLine($"Write Sector {sector} offset {offset} Length{len} bytes.")
        End Using

        Return IOStatus.Success
    End Function


    Public Function WriteFile(ByRef sector As Integer, ByRef block As Integer, ByRef len As Integer, buffer As Byte()) As IOStatus

        Dim r = FindFreeSpace(sector, block, buffer.Length)
        len = buffer.Length
        block = block * blocksize
        If r = IOStatus.Success Then
            WriteDisk(sector, block, buffer)

        Else
            Return IOStatus.Failed
        End If

        Return IOStatus.Success
    End Function


    Public Function WriteDisk(sector As Integer, offset As Integer, buffer As Byte())
        WriteSector(sector, buffer, offset, buffer.Length)
        Dim ms = Math.Floor(offset / blocksize)
        Dim ml = Math.Floor(buffer.Length / blocksize)
        Console.WriteLine($"Using Blocks {ms} To {ms + ml}")
        Dim bits = readmap(sector)
        For t = ms To (ms + ml)
            bits(t) = True
        Next
        writemap(sector, bits)


    End Function


    Public Function readmap(sector) As BitArray
        Using inFile As New FileStream(Filepaths.filepath & $"Sector{sector}.map", FileMode.Open, FileAccess.Read)
            Dim bitbytes(inFile.Length - 1) As Byte
            inFile.Read(bitbytes, 0, inFile.Length)
            Dim bits = New BitArray(bitbytes)
            Return bits
        End Using
    End Function


    Public Function writemap(sector As Integer, map As BitArray)
        Using outFile As New FileStream(Filepaths.filepath & $"Sector{sector}.map", FileMode.Open, FileAccess.Write)
            Dim b((map.Length - 1) / 8 - 1) As Byte
            map.CopyTo(b, 0)
            outFile.Write(b)
        End Using
        Return -1
    End Function

    Public Function getdiskfree() As List(Of Integer)

        Dim ret As New List(Of Integer)
        Dim sctrs = IO.Directory.GetFiles(Filepaths.filepath)
        Dim totalused As Integer
        Dim totalsize As Integer
        For Each sctr In sctrs
            If sctr.IndexOf(".map") > 0 Then
                Using inFile As New FileStream(sctr, FileMode.Open, FileAccess.Read)
                    Dim bitbytes(inFile.Length - 1) As Byte
                    inFile.Read(bitbytes, 0, inFile.Length)
                    Dim bits = New BitArray(bitbytes)
                    Dim i = 0
                    For Each b In bits
                        If b = True Then i += 1
                    Next
                    Dim used = Math.Round(i / bits.Length * 100)
                    Console.WriteLine($"Sector Used Space is {used}")
                    totalused = totalused + i
                    totalsize = totalsize + bits.Length
                End Using
            End If
        Next
        ret.Add(totalused)
        ret.Add(totalsize)

        Return ret

    End Function

    Public Function getmapused(sector) As Integer
        Using inFile As New FileStream(Filepaths.filepath & $"Sector{sector}.map", FileMode.Open, FileAccess.Read)
            Dim bitbytes(inFile.Length - 1) As Byte
            inFile.Read(bitbytes, 0, inFile.Length)
            Dim bits = New BitArray(bitbytes)
            Dim i = 0
            For Each b In bits
                If b = True Then i += 1
            Next
            Dim used = Math.Round(i / bits.Length * 100)
            Console.WriteLine($"Sector Used Space is {used}")
            Return used
        End Using
    End Function


    Public Function DeleteFile(sector, offset, len) As IOStatus

        Dim bits = readmap(sector)
        Dim st = Math.Floor(offset / blocksize)
        Dim ln = Math.Floor(len / blocksize)
        ln = st + ln
        For t = st To ln
            bits(t) = False
        Next
        Console.WriteLine($"Clear Blocks {sector} offset {st} Length{ln} bytes.")
        writemap(sector, bits)
        getmapused(sector)
        Return IOStatus.Success
    End Function

End Class

' This will be moved to the Dokan Helper Class
' and into a database server. We call Server Create return Sector start len
' To read/write we call for server to get/write chunk with sector start offset len
Public Class Path
    Public key As Integer
    Public atribkey As Integer
    Public filepath As String
    Public LastAccessTime As DateTime
    Public LastWriteTime As DateTime
    Public CreationTime As DateTime
    Public attribute As Integer
    Public DiskNum As Integer '/ server ip address
    Public Sector As Integer
    Public offset As Integer
    Public Length As Integer
End Class

Public Class Atribute
    Public key As Integer
    Public Createtime As DateTime
    Public Accesstime As DateTime
    Public Writetime As DateTime
    Public Atributes As Integer
End Class

' Don't need this Diskio going on server

Public Class Disks
    Public key
    Public ip
    Public DiskNum As Integer
    Public LSectors As New List(Of sectors)
End Class

' change this for no database (Server Commands instead)
' Commands New Sector (id).bin have sector map same (id).map
' Server Command for Writing, Reading sectors.. DiskIO procedures can then be called.
Public Class sectors
    Public key
    Public diskkey
    Public sectorid
    Public sectorsize As Integer
    Public sectorpath As String
    Public SectorMap As String

End Class
