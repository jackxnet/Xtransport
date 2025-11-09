Imports System.Text
Imports System
Imports xPxP.ENUMS
Imports System.Net.Sockets
Imports System.Threading.Channels
Imports System.Reflection.Metadata
Public Class xNetPacket ': Implements IDisposable



    Public Structure listener
        Public crosstalkrepeater As String
        Public crosstalkchannel As Short
        Public crosstalkowner As String

    End Structure



    Private Function padme(st As String, totallength As Integer) As String
        For t = st.Length - 1 To totallength - 2
            st = st & vbNullChar
        Next
        Return st
    End Function

    Private Function unpadme(b As Byte(), totallength As Integer) As String

        Dim unpad = Text.Encoding.ASCII.GetString(b, 0, totallength - 1)
        unpad = unpad.Replace(vbNullChar, "")
        unpad = unpad.Replace("""", "")

        Return unpad
    End Function

    Public Function xnettobyte(s As String) As Byte()
        Return Encoding.UTF8.GetBytes(s)
    End Function

    Public Function xnettostring(b As Byte()) As String
        Return Encoding.UTF8.GetString(b)
    End Function

    Public Function hostinfotoparams(hostinfo As XHost) As String
        Dim s As StringBuilder
        s.Append(hostinfo.hostid.ToString & ",")
        s.Append(hostinfo.ip.ToString() & ",")
        s.Append(hostinfo.port & ",")
        s.Append(hostinfo.channel)

        Return s.ToString
    End Function

    Public Function xencodeServiceRequest(x As XHost, c As XClient, callerhost As Integer, callerid As Integer, servicetype As SERVICE_TYPE, messagetype As MESSAGE_TYPE) As Byte()
        Dim params = New StringBuilder
        params.Append(servicetype & ",")
        params.Append(x.hostid & ",")
        params.Append(x.ip & ",")
        params.Append(x.port & ",")
        params.Append(x.channel & ",")
        params.Append(x.netlatency & ",")
        params.Append(x.packetburst)

        'these should come from local
        'params.Append(c.localid)
        'params.Append(c.localname)
        'params.Append(c.useipv6)
        'params.Append(c.broadcast)

        Dim ninfo = Encoding.UTF8.GetBytes(padme(params.ToString, 60))


        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(ROUTE_CODE.CHANNEL))
        body.AddRange(BitConverter.GetBytes(messagetype))
        body.AddRange(BitConverter.GetBytes(callerid))
        body.AddRange(BitConverter.GetBytes(c.channel))
        body.AddRange(BitConverter.GetBytes(c.hostid))
        body.AddRange(BitConverter.GetBytes(c.localid))
        body.AddRange(BitConverter.GetBytes(servicetype))
        body.AddRange(BitConverter.GetBytes(CONTROL_TYPE.REQUEST))
        body.AddRange(BitConverter.GetBytes(0)) 'control#
        body.AddRange(BitConverter.GetBytes(0)) 'ExtraCode
        body.AddRange(ninfo)



        Return body.ToArray
    End Function

    Public Function xencodeServiceReply(x As XHost, c As XClient, callerhost As Integer, callerid As Integer, servicetype As SERVICE_TYPE) As Byte()
        Dim params = New StringBuilder
        params.Append(servicetype & ",")
        params.Append(x.hostid & ",")
        params.Append(x.ip & ",")
        params.Append(x.port & ",")
        params.Append(x.channel & ",")
        params.Append(x.netlatency & ",")
        params.Append(x.packetburst)

        'these should come from local
        'params.Append(c.localid)
        'params.Append(c.localname)
        'params.Append(c.useipv6)
        'params.Append(c.broadcast)

        Dim ninfo = Encoding.UTF8.GetBytes(padme(params.ToString, 60))


        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(ROUTE_CODE.CHANNEL))
        body.AddRange(BitConverter.GetBytes(callerhost))
        body.AddRange(BitConverter.GetBytes(callerid))
        body.AddRange(BitConverter.GetBytes(c.channel))
        body.AddRange(BitConverter.GetBytes(c.hostid))
        body.AddRange(BitConverter.GetBytes(c.localid))
        body.AddRange(BitConverter.GetBytes(servicetype))
        body.AddRange(BitConverter.GetBytes(CONTROL_TYPE.REPLY))
        body.AddRange(BitConverter.GetBytes(0)) 'control#
        body.AddRange(BitConverter.GetBytes(0)) 'ExtraCode
        body.AddRange(ninfo)



        Return body.ToArray
    End Function






    Public Function xencodevoicerequest(localcode, localid, callercode, callerid, channel)

        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(ROUTE_CODE.CHANNEL))
        body.AddRange(BitConverter.GetBytes(localcode))
        body.AddRange(BitConverter.GetBytes(localid))
        body.AddRange(BitConverter.GetBytes(channel))
        body.AddRange(BitConverter.GetBytes(callercode))
        body.AddRange(BitConverter.GetBytes(callerid))
        body.AddRange(BitConverter.GetBytes(MESSAGE_TYPE.VOICE))
        body.AddRange(BitConverter.GetBytes(CONTROL_TYPE.REQUEST))

        Return body.ToArray

    End Function

    Public Function xencodevoicerespond(hostid, localid, callerhost, callerid, hostip, hostchannel, port)
        Dim ninfo = Encoding.UTF8.GetBytes(padme(hostip & ":" & hostchannel & ":" & port, 60))
        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(ROUTE_CODE.CHANNEL))
        body.AddRange(BitConverter.GetBytes(hostid))
        body.AddRange(BitConverter.GetBytes(localid))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(callerhost))
        body.AddRange(BitConverter.GetBytes(callerid))
        body.AddRange(BitConverter.GetBytes(MESSAGE_TYPE.VOICE))
        body.AddRange(BitConverter.GetBytes(CONTROL_TYPE.REPLY))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(ninfo)

        Return body.ToArray

    End Function

    Public Function xencodevoicechannelpacket(b, channel)
        Dim nname = Encoding.UTF8.GetBytes(padme("RAWPACKET", 60))
        Dim plinfo = Encoding.UTF8.GetBytes(padme("VOICE", 42))

        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(ROUTE_CODE.CHANNEL))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(channel))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(MESSAGE_TYPE.VOICE))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(nname)
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(plinfo)
        body.AddRange(b)
        Return body.ToArray

    End Function


    Public Function xencodevidchannelpacket(b, channel)
        Dim nname = Encoding.UTF8.GetBytes(padme("RAWPACKET", 60))
        Dim plinfo = Encoding.UTF8.GetBytes(padme("VIDEO", 42))

        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(ROUTE_CODE.CHANNEL))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(channel))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(MESSAGE_TYPE.VIDEO))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(nname)
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(plinfo)
        body.AddRange(b)
        Return body.ToArray

    End Function


    Public Function xencodemessageframe(x As xnetmessage_frame)

        Dim ninfo = Encoding.UTF8.GetBytes(padme(x.normstring, 60))
        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(x.routingcode))
        body.AddRange(BitConverter.GetBytes(x.hostid))
        body.AddRange(BitConverter.GetBytes(x.localid))
        body.AddRange(BitConverter.GetBytes(x.channelid))
        body.AddRange(BitConverter.GetBytes(x.callerhost))
        body.AddRange(BitConverter.GetBytes(x.callerid))
        body.AddRange(BitConverter.GetBytes(x.messagetype))
        body.AddRange(BitConverter.GetBytes(x.controlcodes))
        body.AddRange(BitConverter.GetBytes(x.xtracodes1))
        body.AddRange(BitConverter.GetBytes(x.xtracodes2))
        body.AddRange(ninfo)
        Return body.ToArray
    End Function



    Public Function xEncodemessage(routecode As ROUTE_CODE, toarea As Integer, tolocal As Integer, channel As Integer, fromarea As Integer, fromid As Integer, netcode As MESSAGE_TYPE, controlcode As CONTROL_TYPE, extracode1 As Integer, extracode2 As Integer, info As String)

        Dim ninfo = Encoding.UTF8.GetBytes(padme(info, 60))
        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(routecode))
        body.AddRange(BitConverter.GetBytes(toarea))
        body.AddRange(BitConverter.GetBytes(tolocal))
        body.AddRange(BitConverter.GetBytes(channel))
        body.AddRange(BitConverter.GetBytes(fromarea))
        body.AddRange(BitConverter.GetBytes(fromid))
        body.AddRange(BitConverter.GetBytes(netcode))
        body.AddRange(BitConverter.GetBytes(controlcode))
        body.AddRange(BitConverter.GetBytes(extracode1))
        body.AddRange(BitConverter.GetBytes(extracode2))
        body.AddRange(ninfo)


        Return body.ToArray
    End Function


    Public Function xencodepayloadframe(x As xnetpayload_frame)

        Dim ninfo = Encoding.UTF8.GetBytes(padme(x.normstring, 60))
        Dim plinfo = Encoding.UTF8.GetBytes(padme(x.paynorm, 42))

        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(x.routingcode))
        body.AddRange(BitConverter.GetBytes(x.hostid))
        body.AddRange(BitConverter.GetBytes(x.localid))
        body.AddRange(BitConverter.GetBytes(x.channelid))
        body.AddRange(BitConverter.GetBytes(x.callerhost))
        body.AddRange(BitConverter.GetBytes(x.callerid))
        body.AddRange(BitConverter.GetBytes(x.messagetype))
        body.AddRange(BitConverter.GetBytes(x.controlcodes))
        body.AddRange(BitConverter.GetBytes(x.xtracodes1))
        body.AddRange(BitConverter.GetBytes(x.xtracodes2))
        body.AddRange(ninfo)
        body.AddRange(BitConverter.GetBytes(x.payloadtype))
        body.AddRange(BitConverter.GetBytes(x.payloadcode))
        body.AddRange(plinfo)
        body.AddRange(x.payload)


        Return body.ToArray
    End Function



    Public Function xEncodepayload(routecode As ROUTE_CODE, toarea As Integer, tolocal As Integer, channel As Integer, fromarea As Integer, fromid As Integer, netcode As MESSAGE_TYPE, controlcode As CONTROL_TYPE, extracode1 As Integer, extracode2 As Integer, name As String, payloadtype As PAYLOAD_TYPE, payloadcodes As PAYLOAD_CODES, payloadinfo As String, payload As Byte())

        Dim nname = Encoding.UTF8.GetBytes(padme(name, 60))
        Dim plinfo = Encoding.UTF8.GetBytes(padme(payloadinfo, 42))
        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(routecode))
        body.AddRange(BitConverter.GetBytes(toarea))
        body.AddRange(BitConverter.GetBytes(tolocal))
        body.AddRange(BitConverter.GetBytes(channel))
        body.AddRange(BitConverter.GetBytes(fromarea))
        body.AddRange(BitConverter.GetBytes(fromid))
        body.AddRange(BitConverter.GetBytes(netcode))
        body.AddRange(BitConverter.GetBytes(controlcode))
        body.AddRange(BitConverter.GetBytes(extracode1))
        body.AddRange(BitConverter.GetBytes(extracode2))
        body.AddRange(nname)
        body.AddRange(BitConverter.GetBytes(payloadtype))
        body.AddRange(BitConverter.GetBytes(payloadcodes))
        body.AddRange(plinfo)
        body.AddRange(payload)


        Return body.ToArray
    End Function


    Public Function xEncodeDiskIO(routecode As ROUTE_CODE, toarea As Integer, tolocal As Integer, channel As Integer, fromarea As Integer, fromid As Integer, netcode As MESSAGE_TYPE, controlcode As CONTROL_TYPE, param1 As Integer, param2 As Integer, param3 As Integer, param4 As Integer, param5 As Integer, name As String, payloadtype As PAYLOAD_TYPE, payloadcodes As PAYLOAD_CODES, payloadinfo As String, payload As Byte())

        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(routecode))
        body.AddRange(BitConverter.GetBytes(toarea))
        body.AddRange(BitConverter.GetBytes(tolocal))
        body.AddRange(BitConverter.GetBytes(channel))
        body.AddRange(BitConverter.GetBytes(fromarea))
        body.AddRange(BitConverter.GetBytes(fromid))
        body.AddRange(BitConverter.GetBytes(netcode))
        body.AddRange(BitConverter.GetBytes(controlcode))
        body.AddRange(BitConverter.GetBytes(param1))
        body.AddRange(BitConverter.GetBytes(param2))
        body.AddRange(BitConverter.GetBytes(param3))
        body.AddRange(BitConverter.GetBytes(param4))
        body.AddRange(BitConverter.GetBytes(param5))
        body.AddRange(payload)


        Return body.ToArray
    End Function

    Public Function xDecodeDiskIO(b As Byte()) As xnetDiskIO_frame
        Dim xnet As New xnetDiskIO_frame
        ReDim xnet.payload(b.Length - 53)
        xnet.routingcode = BitConverter.ToInt32(b, 0)
        xnet.hostid = BitConverter.ToInt32(b, 4)
        xnet.localid = BitConverter.ToInt32(b, 8)
        xnet.channelid = BitConverter.ToInt32(b, 12)
        xnet.callerhost = BitConverter.ToInt32(b, 16)
        xnet.callerid = BitConverter.ToInt32(b, 20)
        xnet.messagetype = BitConverter.ToInt32(b, 24)
        xnet.controlcodes = BitConverter.ToInt32(b, 28)
        xnet.param1 = BitConverter.ToInt32(b, 32)
        xnet.param2 = BitConverter.ToInt32(b, 36)
        xnet.param3 = BitConverter.ToInt32(b, 40)
        xnet.param4 = BitConverter.ToInt32(b, 44)
        xnet.param5 = BitConverter.ToInt32(b, 48)
        Array.Copy(b, 52, xnet.payload, 0, xnet.payload.Length)

        Return xnet

    End Function



    Public Function xDecodeRoute(b As Byte()) As xnetroute_frame
        Dim xnet As New xnetroute_frame
        xnet.routingcode = BitConverter.ToInt32(b, 0)
        xnet.hostid = BitConverter.ToInt32(b, 4)
        xnet.localid = BitConverter.ToInt32(b, 8)
        xnet.channelid = BitConverter.ToInt32(b, 12)
        xnet.callerhost = BitConverter.ToInt32(b, 16)
        xnet.callerid = BitConverter.ToInt32(b, 20)
        xnet.messagetype = BitConverter.ToInt32(b, 24)
        Return xnet
    End Function



    Public Function xDecodemessage(b As Byte()) As xnetmessage_frame
        Dim xnet As New xnetmessage_frame
        xnet.routingcode = BitConverter.ToInt32(b, 0)
        xnet.hostid = BitConverter.ToInt32(b, 4)
        xnet.localid = BitConverter.ToInt32(b, 8)
        xnet.channelid = BitConverter.ToInt32(b, 12)
        xnet.callerhost = BitConverter.ToInt32(b, 16)
        xnet.callerid = BitConverter.ToInt32(b, 20)
        xnet.messagetype = BitConverter.ToInt32(b, 24)
        xnet.controlcodes = BitConverter.ToInt32(b, 28)
        xnet.xtracodes1 = BitConverter.ToInt32(b, 32)
        xnet.xtracodes2 = BitConverter.ToInt32(b, 36)
        Dim norm(59) As Byte
        Array.Copy(b, 40, norm, 0, 59)
        xnet.normstring = unpadme(norm, 60)

        Return xnet

    End Function

    Public Function xDecodepayload(b As Byte()) As xnetpayload_frame
        Dim xnet As New xnetpayload_frame
        ReDim xnet.payload(b.Length - 151)
        xnet.routingcode = BitConverter.ToInt32(b, 0)
        xnet.hostid = BitConverter.ToInt32(b, 4)
        xnet.localid = BitConverter.ToInt32(b, 8)
        xnet.channelid = BitConverter.ToInt32(b, 12)
        xnet.callerhost = BitConverter.ToInt32(b, 16)
        xnet.callerid = BitConverter.ToInt32(b, 20)
        xnet.messagetype = BitConverter.ToInt32(b, 24)
        xnet.controlcodes = BitConverter.ToInt32(b, 28)
        xnet.xtracodes1 = BitConverter.ToInt32(b, 32)
        xnet.xtracodes2 = BitConverter.ToInt32(b, 36)
        Dim norm(59) As Byte
        Array.Copy(b, 40, norm, 0, 59)
        xnet.normstring = unpadme(norm, 60)
        xnet.payloadtype = BitConverter.ToInt32(b, 100)
        xnet.payloadcode = BitConverter.ToInt32(b, 104)
        Dim pinfo(41) As Byte
        Array.Copy(b, 108, pinfo, 0, 41)
        xnet.paynorm = unpadme(pinfo, 42)
        Array.Copy(b, 150, xnet.payload, 0, xnet.payload.Length)

        Return xnet

    End Function




    Public Function xDecodemessagetolorastring(b As Byte()) As String
        Dim xnet As New xnetmessage_frame
        xnet.routingcode = BitConverter.ToInt32(b, 0)
        xnet.hostid = BitConverter.ToInt32(b, 4)
        xnet.localid = BitConverter.ToInt32(b, 8)
        xnet.channelid = BitConverter.ToInt32(b, 12)
        xnet.callerhost = BitConverter.ToInt32(b, 16)
        xnet.callerid = BitConverter.ToInt32(b, 20)
        xnet.messagetype = BitConverter.ToInt32(b, 24)
        xnet.controlcodes = BitConverter.ToInt32(b, 28)
        xnet.xtracodes1 = BitConverter.ToInt32(b, 32)
        xnet.xtracodes2 = BitConverter.ToInt32(b, 36)
        Dim norm(59) As Byte
        Array.Copy(b, 40, norm, 0, 59)
        xnet.normstring = unpadme(norm, 60)


        Dim lora As New StringBuilder

        lora.Append(xnet.hostid & ":")
        lora.Append(xnet.callerid & ":")
        lora.Append(xnet.normstring)
        Dim slora As String = lora.ToString
        Dim loralen = slora.Length
        Dim rlora As String
        lora.Clear()
        lora.Append(xnet.channelid & ",")
        lora.Append(loralen & ",")
        lora.Append(slora)


        Return lora.ToString

    End Function


    Public Function lorastringtoxmessage(ls As String) As Byte()

        Dim lsplit = ls.Split(",")
        Dim lsmesg = lsplit(2).Split(":")

        Dim channel = lsplit(0)
        Dim callercode = lsmesg(0)
        Dim callerid = lsmesg(1)
        Dim dmsg = lsmesg(2)

        Dim nname = Encoding.UTF8.GetBytes(padme(dmsg, 60))
        Dim body As New List(Of Byte)
        body.AddRange(BitConverter.GetBytes(ROUTE_CODE.CHANNEL))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(Val(channel)))
        body.AddRange(BitConverter.GetBytes(Val(callercode)))
        body.AddRange(BitConverter.GetBytes(Val(callerid)))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(BitConverter.GetBytes(0))
        body.AddRange(nname)

        Return body.ToArray

    End Function


    'Public Sub Dispose() Implements IDisposable.Dispose
    '     GC.Collect()
    'End Sub
End Class


Public Class xnetmessage_frame

    Public routingcode As Integer
    Public hostid As Integer
    Public localid As Integer
    Public channelid As Integer
    Public callerhost As Integer
    Public callerid As Integer
    Public messagetype As Integer
    Public controlcodes As Integer
    Public xtracodes1 As Integer
    Public xtracodes2 As Integer
    Public normstring As String = ""
    Public timestamp As DateTime

End Class


Public Class xnetpayload_frame

    Public routingcode As Integer
    Public hostid As Integer
    Public localid As Integer
    Public channelid As Integer
    Public callerhost As Integer
    Public callerid As Integer
    Public messagetype As Integer
    Public controlcodes As Integer
    Public xtracodes1 As Integer
    Public xtracodes2 As Integer
    Public normstring As String
    Public payloadtype As Integer
    Public payloadcode As Integer
    Public paynorm As String = ""
    Public payload As Byte()
    Public timestamp As DateTime

End Class

Public Class xnetDiskIO_frame

    Public routingcode As Integer
    Public hostid As Integer
    Public localid As Integer
    Public channelid As Integer
    Public callerhost As Integer
    Public callerid As Integer
    Public messagetype As Integer
    Public controlcodes As Integer
    Public param1 As Integer
    Public param2 As Integer
    Public param3 As Integer
    Public param4 As Integer
    Public param5 As Integer
    Public normstring As String
    Public payloadtype As Integer
    Public payloadcode As Integer
    Public paynorm As String = ""
    Public payload As Byte()
    Public timestamp As DateTime

End Class



Public Class xnetroute_frame

    Public routingcode As Integer
    Public hostid As Integer
    Public localid As Integer
    Public channelid As Integer
    Public callerhost As Integer
    Public callerid As Integer
    Public messagetype As Integer
End Class

