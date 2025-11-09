Imports xnethost
Imports xPxP
Imports System.Threading.Thread
Imports xPxP.ENUMS
Imports System.Xml
Imports xPxP.xNetPacket
Imports xnethost.serversocks
Imports xnethost.GDictionaries
Imports Windows.Win32.System
Imports System.Data
Imports System.Reflection
Imports System.Runtime.Intrinsics
Imports xnet_dbase_service.queryengine
Imports System.IO
Imports System.Net
Imports System.Text
Imports System
Imports Newtonsoft.Json
Imports System.Threading
Public Class DbaseService
    Implements IServicePlugin
    Private msgr As messengerService
    Private HostServices = New Dictionary(Of Integer, HostService)
    Private ClientServices = New Dictionary(Of Integer, ClientService)
    Private otherServices = New Dictionary(Of Integer, OtherService)
    Private dpsvr As New dispatchersvr

    Function importtable()
        If debugon Then Console.WriteLine("Loading import")

        Dim reader As New StreamReader(Filepaths.dbasepath & "cellcontacts.json", Encoding.Default)
        Dim a As String
        Dim d As String = ""
        Dim dc = New DbaseClient
        Dim json As String
        If debugon Then Console.WriteLine("Starting import")

        dc.loadquery("DC°cellcontacts")
        dc.run_query()

        'Dim b = a.Replace(vbTab, "·")
        For z = 0 To 655
            json = ""
            For t = 0 To 4
                a = reader.ReadLine
                json = json & a
            Next

            json = json.Substring(0, json.Length - 1)

            Dim otxtlg As cellcontacts = JsonConvert.DeserializeObject(Of cellcontacts)(json)

            Dim newrec = otxtlg.customerid & "·" & otxtlg.customername & "·" & otxtlg.cellphone

            'Dim b = a.Replace(vbTab, "·")
            Dim c = "DA°cellcontacts∙"
            d = c & newrec
            dc.loadquery(d)
            dc.run_query()

        Next z

        reader.Close()
        If debugon Then Console.WriteLine("Finishing import")

        Return d
    End Function



    Public Sub addhostservice(params) Implements IServicePlugin.addhostservice
        'Dim hs As New HostService
        ' Dim p = Split(params, ",")
        'hs.doproccess()
        'importtable()
        Dim qc = New QueryClient
        qc.start_service()
        Task.Run(Sub() dataready_host())
        Task.Run(Sub() dpsvr.dataDispatcher())
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
        If debugon Then Console.WriteLine("Plugin Dbase Service v1.1a Running")
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

    Private Sub recxpayload(b As Byte())
        Dim xf As New xnetpayload_frame
        Dim xnet As New xNetPacket
        xf = xnet.xDecodepayload(b)


        If xf.payloadtype = PAYLOAD_TYPE.SQL Then

            If xf.controlcodes = SQL_TYPE.QUERY Then
                Task.Run(Sub()
                             Dim qc As New QueryClient
                             qc.localid = xf.callerid
                             qc.hostid = xf.callerhost
                             qc.Queryid = xf.xtracodes1
                             qc.loadquerys(System.Text.Encoding.UTF8.GetString(xf.payload))
                             qc.runqueries()
                         End Sub)

            Else
                DbaseQue.Enqueue(b)
            End If

        End If
    End Sub


    Private Sub dataready_host()
        While canceltoken = False
            While data_ready_host.Count > 0
                Dim b As Byte() = Nothing
                Dim r = data_ready_host.TryDequeue(b)
                If r = True Then
                    Dim xf As New xnetmessage_frame
                    Dim xnet As New xNetPacket
                    xf = xnet.xDecodemessage(b)
                    If debugon Then Console.WriteLine($"Got xtransfer Packet with ID {xf.xtracodes1} {xf.xtracodes2}")

                    If xf.messagetype = MESSAGE_TYPE.PAYLOAD Then
                        recxpayload(b)
                    End If
                End If
            End While
            Sleep(10)
        End While
    End Sub




End Class


Public Class dispatchersvr
    Private dbsvr As New DbaseClient

    Public localid
    Public hostid
    Public Queryid
    Public Sub dataDispatcher()
        While canceltoken = False
            While DbaseQue.Count > 0
                Dim b As Byte() = Nothing
                Dim r = DbaseQue.TryDequeue(b)
                If r = True Then
                    Dim xf As New xnetpayload_frame
                    Dim xnet As New xNetPacket
                    xf = xnet.xDecodepayload(b)
                    localid = xf.callerid
                    hostid = xf.callerhost
                    Queryid = xf.xtracodes1
                    dbsvr.loadquery(System.Text.Encoding.UTF8.GetString(xf.payload))
                    Dim retb = dbsvr.run_query()

                    Try
                        Dim xpacket As New xNetPacket
                        Dim msg = xpacket.xEncodemessage(ROUTE_CODE.UNICAST, hostid, localid, 0, 0, 0, MESSAGE_TYPE.SQL, SQL_TYPE.NONQUERY, Queryid, retb, $"DbaseHost return code {retb}")
                        If debugon Then Console.WriteLine($"Sending return code {retb}")
                        serversock.routeunicast(localid, msg)
                    Catch ex As Exception
                    End Try


                End If
            End While
            Sleep(10)
        End While
    End Sub

    Private Sub rtnsender(xpckt As xnetpayload_frame)

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



    Private Sub recxpayload(b As Byte())
        Dim xf As New xnetpayload_frame
        Dim xnet As New xNetPacket
        xf = xnet.xDecodepayload(b)


    End Sub

    Public Sub processdata(msg)
        Dim xf As New xnetmessage_frame
        Dim xnet As New xNetPacket
        xf = xnet.xDecodemessage(msg)

        If xf.messagetype = MESSAGE_TYPE.PAYLOAD Then

        End If


        If xf.controlcodes = CONTROL_TYPE.REQUEST Then

            Select Case xf.messagetype


            End Select
        End If


        'if debugon then console.writeline("Got a Msg")

    End Sub

End Class

Public Class OtherService
    Public id As Integer
    Public canceltoken As Boolean


    Public Sub processdata(msg)

    End Sub

End Class


Public Class HostService
    Public id As Integer
    Public canceltoken As Boolean



    Public Sub doproccess()
    End Sub


End Class

Public Class ClientService
    Public id As Integer
    Public Sub StartService()

    End Sub

End Class


Public Class tmptable
    Public tablename
    Public colnames As New List(Of String)
    Public rows As New List(Of String())
End Class


Public Class QueryClient

    Private q As New queryengine
    Private ttables As New List(Of tmptable)
    Private querys As New List(Of String)
    Private ct As New tmptable
    Private llogic As New List(Of queryengine.compare)
    Public localid As Integer
    Public hostid As Integer
    Public Queryid As Integer

    ' This function for retrieving current tables index
    Private Function ColnameorNum(colname As String)
        Dim dummyd As Decimal = 0
        Dim reslt = Decimal.TryParse(colname, dummyd)
        If reslt = True Then Return dummyd

        For t = 0 To ct.colnames.Count - 1
            If colname = ct.colnames(t) Then
                Return t
            End If
        Next
        Return -1
    End Function

    Private Function ColnameorNum(colname As String, idx As String)
        Dim dummyd As Decimal = 0
        Dim reslt = Decimal.TryParse(colname, dummyd)
        If reslt = True Then Return dummyd

        For t = 0 To ttables(idx).colnames.Count - 1
            If colname = ttables(idx).colnames(t) Then
                Return t
            End If
        Next
        Return -1
    End Function

    ' used to get temptable index by name
    Private Function TableNameorNum(tblname As String)
        Dim dummyd As Decimal = 0
        Dim reslt = Decimal.TryParse(tblname, dummyd)
        If reslt = True Then Return dummyd

        For t = 0 To ttables.Count - 1
            If ttables(t).tablename = tblname Then
                Return t
            End If
        Next
        Return -1
    End Function


    Public Sub loadquerys(q As String)
        querys.Clear()
        Dim questrings = Split(q, "≈")

        For Each que In questrings
            querys.Add(que)
        Next

    End Sub

    Private Function orderby(ByVal tbl As tmptable, tp As Char, fn As String, ob As String) As tmptable

        Dim f = ColnameorNum(fn)

        Dim result As New tmptable
        result = tbl
        result.colnames = tbl.colnames
        If tp = "V" Then
            If ob.ToUpper = "ASC" Then
                tbl.rows.Sort(Function(x, y) Val(x(f)).CompareTo(Val(y(f))))
            ElseIf ob.ToUpper = "DESC" Then
                tbl.rows.Sort(Function(x, y) Val(y(f)).CompareTo(Val(x(f))))
            End If
        End If


        If tp = "T" Then
            If ob.ToUpper = "ASC".ToUpper Then
                tbl.rows.Sort(Function(x, y) x(f).CompareTo(y(f)))
            ElseIf ob.ToUpper = "DESC" Then
                tbl.rows.Sort(Function(x, y) y(f).CompareTo(x(f)))
            End If
        End If

        If tp = "D" Then
            If ob.ToUpper = "ASC".ToUpper Then
                tbl.rows.Sort(Function(x, y) DateTime.Parse(x(f)).CompareTo(DateTime.Parse(y(f))))
            ElseIf ob.ToUpper = "DESC" Then
                tbl.rows.Sort(Function(x, y) DateTime.Parse(y(f)).CompareTo(DateTime.Parse(x(f))))
            End If
        End If

        Return result


        While tbl.rows.Count > 0
            Dim high = 0
            Dim highval = tbl.rows(0)(f)
            For t = 0 To tbl.rows.Count - 1
                If tp = "V" Then
                    If ob = "asc" Or ob = "ASC" Then
                        If Val(highval) > Val(tbl.rows(t)(f)) Then
                            high = t
                            highval = tbl.rows(t)(f)
                        End If
                    Else
                        If Val(highval) < Val(tbl.rows(t)(f)) Then
                            high = t
                            highval = tbl.rows(t)(f)
                        End If
                    End If
                Else
                    If ob = "asc" Or ob = "ASC" Then
                        If (highval) > (tbl.rows(t)(f)) Then
                            highval = tbl.rows(t)(f)
                            high = t
                        End If
                    Else
                        If (highval) < (tbl.rows(t)(f)) Then
                            high = t
                            highval = tbl.rows(t)(f)
                        End If
                    End If
                End If

            Next
            result.rows.Add(tbl.rows(high))
            'if debugon then console.writeline($"adding lowest {db(high)(f)}")

            tbl.rows.RemoveAt(high)
        End While


        Return result
    End Function

    Private Function getUniqueFields(tbl As tmptable, fn As String) As tmptable

        Dim f = ColnameorNum(fn)

        Dim result = New tmptable
        result.colnames = tbl.colnames

        Dim found = False
        For Each entry In tbl.rows
            For t = 0 To result.rows.Count - 1
                If entry(f) Is result.rows(t) Then found = True
                Exit For
            Next
            If found = False Then result.rows.Add(entry)
        Next
        Return result

    End Function


    Private Function limit(tbl As tmptable, l As Integer) As tmptable

        Dim result = New tmptable
        result.colnames = tbl.colnames

        For t = 0 To tbl.rows.Count - 1
            result.rows.Add(tbl.rows(t))
            If t = l - 1 Then
                Exit For
            End If
        Next

        Return result
    End Function

    Private Function FindTableNumber(tblname) As Integer
        For t = 0 To tablelist.Count - 1
            If tablelist(t).tablename = tblname Then
                Return t
                Exit For
            End If
        Next
    End Function

    Private Sub logic_push(t, f, cf, v)

        Dim cfl As queryengine.Cflags
        [Enum].TryParse(cf, cfl)
        Dim cmp As queryengine.compare
        cmp.type = t
        cmp.field = f
        cmp.cflag = cfl
        cmp.fieldval = v
        llogic.Add(cmp)
    End Sub

    Private Sub Logic_PushTable(Ltype, fldname, eq, tbi, coli)

        Dim i = TableNameorNum(tbi)
        Dim ci = ColnameorNum(coli, i)

        For Each row In ttables(i).rows
            logic_push(Ltype, fldname, eq, row(ci))
        Next

    End Sub

    Private Function rowcompare(row1 As String(), row2 As String())
        If llogic.Count = 0 Then Return False
        Dim returnbool = True
        Dim rts = New List(Of Boolean)

        For Each tc In llogic

            Select Case tc.cflag
                Case 0
                    If row1(tc.field) = row2(tc.fieldval) Then
                        rts.Add(True)
                    Else
                        rts.Add(False)
                    End If
                Case 2
                    If row1(tc.field) > row2(tc.fieldval) Then
                        rts.Add(True)
                    Else
                        rts.Add(False)
                    End If

                Case 3
                    If row1(tc.field) < row2(tc.fieldval) Then
                        rts.Add(True)
                    Else
                        rts.Add(False)
                    End If
            End Select

        Next


        For Each rtt In rts
            If rtt = False Then
                returnbool = False
                Exit For
            End If
        Next
        Return returnbool

    End Function


    Private Function table_replace(tname1, tname2, newtblname) As tmptable
        Dim rt As New tmptable
        rt.tablename = newtblname

        Dim t1 = TableNameorNum(tname1)
        Dim t2 = TableNameorNum(tname2)

        ' have to swap a copy structs are byref and won't update
        For t = 0 To llogic.Count - 1
            Dim newcomp As New queryengine.compare
            newcomp = llogic(t)
            Dim checkfield = ColnameorNum(newcomp.field, t1)
            newcomp.setfield(ColnameorNum(checkfield))
            newcomp.fieldval = (ColnameorNum(checkfield, t1))
            llogic(t) = newcomp
        Next


        For Each row In ttables(t1).rows
            Dim found = False
            For Each row2 In ttables(t2).rows
                If rowcompare(row, row2) Then
                    rt.rows.Add(row2)
                    found = True
                    Exit For
                End If

            Next
            If found = False Then
                rt.rows.Add(row)
            End If
        Next
        Return rt
    End Function



    Private Function table_join(tname1 As String, tname2 As String, col1 As String, col2 As String, newtblname As String) As tmptable
        Dim rt As New tmptable

        Try
            rt.tablename = newtblname
            Dim t1 = TableNameorNum(tname1)
            Dim t2 = TableNameorNum(tname2)
            Dim c1 = ColnameorNum(col1, t1)
            Dim c2 = ColnameorNum(col2, t2)

            For Each row1 In ttables(t1).rows
                For Each row2 In ttables(t2).rows
                    If row1(c1) = row2(c2) Then
                        Dim newrow(row1.Length + row2.Length - 1) As String
                        row1.CopyTo(newrow, 0)
                        row2.CopyTo(newrow, row1.Length)
                        rt.rows.Add(newrow)
                        'if debugon then console.writeline($"Row {row1(c1)} to {row2(c2)}  joined")
                    Else
                    End If
                Next
            Next
            Return rt
        Catch ex As Exception
            If debugon Then Console.WriteLine("We died in Table Joine")
        End Try

    End Function

    Public Sub load_query_logic(logic As String)

        If logic.ToUpper = "AND" Then
            q.andcomparitors = llogic
        End If
        If logic.ToUpper = "OR" Then
            q.orcomparitors = llogic
        End If
    End Sub



    Public Function runqueries()

        For Each qq In querys

            Try

                Dim cmd = Split(qq, "°")
                Select Case cmd(0)

                    ' START COMPARITOR ROUTINES
                    Case "LP"
                        Dim params = Split(cmd(1), "∙")
                        logic_push(params(0), params(1), params(2), params(3))

                    Case "LPT"
                        Dim params = Split(cmd(1), "∙")
                        Logic_PushTable(params(0), params(1), params(2), params(3), params(4))

                    Case "LC"
                        llogic.Clear()

                        'START CURRENT TEMP TABLE ROUTINES

                    Case "CTU" ' Unique key
                        Dim params = Split(cmd(1), "∙")
                        ct = uniquekey(params(0), ct)
                        If debugon Then Console.WriteLine($"Got Unique table with {ct.rows.Count} rows")

                    Case "CTO" ' ORDER BY
                        Dim params = Split(cmd(1), "∙")
                        ct = orderby(ct, params(0), params(1), params(2))
                        If debugon Then Console.WriteLine($"Got Ordered table with {ct.rows.Count} rows")

                    Case "CTL" ' Limit table
                        Dim params = Split(cmd(1), "∙")
                        ct = limit(ct, params(0))
                        If debugon Then Console.WriteLine($"Got Limited table with {ct.rows.Count} rows")

                    Case "CTP" ' (Push) Add Current table to list of temp tables
                        Dim nt = New tmptable
                        For Each row In ct.rows
                            nt.rows.Add(row)
                        Next
                        nt.tablename = ct.tablename
                        nt.colnames = ct.colnames
                        ttables.Add(nt)
                        ct = New tmptable

                    Case "CSN" 'Current Set Name
                        Dim params = Split(cmd(1), "∙")
                        ct.tablename = params(0)

                    Case "CTC" 'Clear Current Table
                        ct = New tmptable


                    Case "CTR" 'Columns to Return
                        Dim params = Split(cmd(1), "∙")
                        q.cols = Split(params(0), "·")

                        'FINISH CURRENT TEMP TABLE ROUTINES

                    Case "TTR"
                        Dim p = Split(cmd(1), "∙")
                        ct = table_replace(p(0), p(1), p(2))
                        If debugon Then Console.WriteLine($"Got Replaced table with {ct.rows.Count} rows")

                    Case "TTJ"
                        Dim p = Split(cmd(1), "∙")
                        ct = table_join(p(0), p(1), p(2), p(3), p(4))
                        If debugon Then Console.WriteLine($"Got Joined table {p(0)} to table {p(1)} with {ct.rows.Count} rows")


                        ' Start DataEngine Routines

                    Case "DLL"
                        If cmd(1) = "AND" Then
                            For Each row In llogic
                                q.andcomparitors.Add(row)
                            Next
                        End If
                        If cmd(1) = "OR" Then
                            For Each row In llogic
                                q.orcomparitors.Add(row)
                            Next
                        End If

                    Case "DCL" ' Clear Query Engine
                        q.andcomparitors.Clear()
                        q.orcomparitors.Clear()

                    Case "DQ" ' DATA QUERY ENGINE
                        ct = q.run_query(cmd(1))
                        If debugon Then Console.WriteLine($"Got working table {cmd(1)} with {ct.rows.Count} rows")
                        q.andcomparitors.Clear()
                        q.orcomparitors.Clear()

                    Case "DGH" ' Get Column Headers/Names
                        Dim p = Split(cmd(1), "∙")
                        Dim h As String()
                        For t = 0 To tablelist.Count - 1
                            If tablelist(t).tablename = p(0) Then
                                h = tablelist(t).getcolnames
                                Exit For
                            End If
                        Next
                        ct.rows.Add(h)
                        If debugon Then Console.WriteLine($"Got Column Names for {p(0)}")


                        'RETURN TABLE ? ERROR ROUTINES

                    Case "RT"

                        If ct.rows.Count = 0 Then
                            Dim xpacket As New xNetPacket
                            ct.rows.Add({"0", "No Rows Found"})
                            Dim b = xpacket.xEncodepayload(ROUTE_CODE.UNICAST, hostid, localid, 0, 0, 0, MESSAGE_TYPE.PAYLOAD, SQL_TYPE.QUERY, Queryid, 0, "DbaseHost", PAYLOAD_TYPE.SQL, SQL_TYPE.QUERY, 0, listtobyte(ct.rows))
                            serversock.routeunicast(localid, b)
                            Exit Function
                        End If
                        Try
                            Dim xpacket As New xNetPacket
                            Dim b = xpacket.xEncodepayload(ROUTE_CODE.UNICAST, hostid, localid, 0, 0, 0, MESSAGE_TYPE.PAYLOAD, SQL_TYPE.QUERY, Queryid, 0, "DbaseHost", PAYLOAD_TYPE.SQL, SQL_TYPE.QUERY, 0, listtobyte(ct.rows))
                            serversock.routeunicast(localid, b)
                        Catch ex As Exception
                            If debugon Then Console.WriteLine("We Died a horrible DEATH!")
                        End Try

                End Select
            Catch ex As Exception
                Try
                    Dim xpacket As New xNetPacket
                    Dim er As New List(Of String())
                    er.Add({"ERROR BAD REQUET"})
                    Dim b = xpacket.xEncodepayload(ROUTE_CODE.UNICAST, hostid, localid, 0, 0, 0, MESSAGE_TYPE.PAYLOAD, 0, Queryid, 0, "DbaseHost", PAYLOAD_TYPE.SQL, 0, 0, listtobyte(er))
                    serversock.routeunicast(localid, b)
                    Exit Function
                Catch exx As Exception
                    'We are fucked!
                    If debugon Then Console.WriteLine("End of Line Boys")
                    Exit Function
                End Try

            End Try

        Next

    End Function

    Private Function listtobyte(l As List(Of String())) As Byte()

        Dim sb As New StringBuilder

        For Each row In l
            For Each col In row
                sb.Append(col & "·")
            Next
            sb.Remove(sb.Length - 1, 1)
            sb.Append("∙")
        Next
        sb.Remove(sb.Length - 1, 1)
        Return Encoding.UTF8.GetBytes(sb.ToString)
    End Function


    Private Function listtodatatable(l As List(Of String())) As DataTable

        Dim dt As New DataTable
        For t = 0 To l(0).Count
            dt.Columns.Add(t.ToString)
        Next

        For Each row In l
            dt.Rows.Add(row)
        Next

        Return dt
    End Function
    Private Function tabletobyte(dt)
        Dim stream As System.IO.MemoryStream = New System.IO.MemoryStream()
        Dim formatter As System.Runtime.Serialization.IFormatter = New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
        formatter.Serialize(stream, dt)
        Dim bytes As Byte() = stream.GetBuffer()
        Return bytes
    End Function

    Private Function bytetotable(b As Byte())
        Dim stream As System.IO.MemoryStream = New System.IO.MemoryStream(b)
        Dim formatter As System.Runtime.Serialization.IFormatter = New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
        Dim dt As DataTable = formatter.Deserialize(stream)
        Return dt

    End Function






    Private Function uniquekey(keyfield As String, tbl As tmptable) As tmptable

        Dim f As Integer = ColnameorNum(keyfield)

        Dim rt As New tmptable

        Dim r As List(Of String()) = tbl.rows.DistinctBy(Function(x) x(f)).ToList()

        rt.colnames = tbl.colnames
        rt.rows = r


        Return rt

        Dim found As Boolean = False
        For Each row In tbl.rows
            found = False
            For t = 0 To rt.rows.Count - 1
                If row(f) = rt.rows(t)(f) Then
                    found = True
                    Exit For
                End If
            Next
            If found = False Then
                rt.rows.Add(row)
            End If
        Next
        Return rt
    End Function

    Public Sub createtable(tblname As String)
        Dim t As New ptable
        't.insert_que($"TC°{tblname}∙")
        ' t.createtable(tblname)
        tablelist.Add(t)
        If debugon Then Console.WriteLine($"Added table {tblname}")
    End Sub


    Public Function settypes(tbl, r)
        Dim rid As Integer
        For Each t In tablelist
            If t.tablename = tbl Then
                't.setcoltypes(r)
                If debugon Then Console.WriteLine($"Set Types for {tbl}")
            End If
        Next
        Return rid
    End Function


    Public Function setcolnames(tbl, r)
        Dim rid As Integer
        For Each t In tablelist
            If t.tablename = tbl Then
                t.setcolnames(r)
                If debugon Then Console.WriteLine($"Set Column Names for {tbl}")
            End If
        Next
        Return rid
    End Function


    Private Function docompare(row As String(), cindex As Integer)
        If row Is Nothing Then Return False
        If llogic(cindex).type = "V" Then
            Try
                Dim v As Decimal
                Dim r = Decimal.TryParse(llogic(cindex).fieldval, v)
                If r = False Then Return False
            Catch ex As Exception
                Return (False)
            End Try
        End If
        Dim returnbool = False
        If llogic(cindex).type = "V" Then

            Select Case llogic(cindex).cflag
                Case 0

                    If Val(row(llogic(cindex).field)) = Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If Val(row(llogic(cindex).field)) <> Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If Val(row(llogic(cindex).field)) > Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If Val(row(llogic(cindex).field)) < Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        Else

            Select Case llogic(cindex).cflag
                Case 0

                    If (row(llogic(cindex).field)) = (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If (row(llogic(cindex).field)) <> (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If (row(llogic(cindex).field)) > (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If (row(llogic(cindex).field)) < (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        End If

        Return returnbool
    End Function
    Public Sub start_service()
        Dim files = IO.Directory.GetFiles(Filepaths.dbasepath)

        For Each file In files
            Dim fn = System.IO.Path.GetFileName(file)
            Dim fns = Split(fn, ".")
            If fns(1) = "bin" Then
                Dim pt As New ptable
                pt.tablename = fns(0)
                pt.load()
                pt.loadinf()
                tablelist.Add(pt)
            End If
        Next


    End Sub

    Public Function get_table_count()
        Return tablelist.Count
    End Function

End Class

Public Class modfield
    Public columnnumber As String
    Public columnvalue As String
End Class


Public Class DbaseClient

    Private llogic As New List(Of queryengine.compare)
    Private modfields As New List(Of modfield)
    Public localid As Integer
    Public hostid As Integer
    Public Queryid As Integer
    Private querys As New List(Of String)

    Private Function ColnameorNum(colname As String, idx As String)
        Dim dummyd As Decimal = 0
        Dim reslt = Decimal.TryParse(colname, dummyd)
        If reslt = True Then Return dummyd
        Dim columns As String() = tablelist(idx).getcolnames

        For t = 0 To columns.Count - 1
            If colname = columns(t) Then
                Return t
            End If
        Next
        Return -1
    End Function

    ' for tablelist dbaselogic
    Private Sub setllogicfieldstonum(tbl)
        For t = 0 To llogic.Count - 1
            Dim newcomp As New queryengine.compare
            newcomp = llogic(t)
            Dim checkfield = ColnameorNum(newcomp.field, tbl)
            newcomp.setfield(checkfield)
            llogic(t) = newcomp
        Next
    End Sub


    Private Sub ModFieldPush(colnum, colval)
        Dim mf = New modfield
        mf.columnnumber = colnum
        mf.columnvalue = colval
        modfields.Add(mf)
    End Sub

    Private Sub logic_push(t, f, cf, v)
        Dim cfl As queryengine.Cflags
        [Enum].TryParse(cf, cfl)
        Dim cmp As queryengine.compare
        cmp.type = t
        cmp.field = f
        cmp.cflag = cfl
        cmp.fieldval = v
        llogic.Add(cmp)
    End Sub

    Public Function gettableindex(tblname)
        For t = 0 To tablelist.Count - 1
            If tablelist(t).tablename = tblname Then
                Return t
            End If
        Next
        Return -1
    End Function

    Public Sub loadquery(q As String)
        querys.Clear()
        Dim questrings = Split(q, "≈")
        For Each que In questrings
            querys.Add(que)
        Next

    End Sub


    Public Function run_query()
        llogic.Clear()
        modfields.Clear()
        For Each qq In querys
            If debugon Then Console.WriteLine($"Got a Query {qq}")
            Try

                Dim cmd = Split(qq, "°")


                Select Case cmd(0)

                    Case "LP"
                        Dim params = Split(cmd(1), "∙")
                        logic_push(params(0), params(1), params(2), params(3))


                    Case "LC"
                        llogic.Clear()

                    Case "MF"
                        Dim params = Split(cmd(1), "∙")
                        ModFieldPush(params(0), params(1))

                    Case "MFC"
                        modfields.Clear()

                    Case "DC"
                        Dim p = Split(cmd(1), "∙")
                        Dim db As New ptable
                        db.createtable(p(0))
                        Console.WriteLine($"New Table Created {p(0)}")
                        Return (returncodes.TABLE_CREATE_SUCCESS)



                    Case "DA"
                        Dim p = Split(cmd(1), "∙")
                        Dim r = Split(p(1), "·")
                        Dim rid = addrow(p(0), r)
                        If rid > 0 Then
                            Console.WriteLine($"New Row Added key {rid}")
                            Return (rid)
                        Else
                            Return (returncodes.ROW_ADD_ERROR)
                        End If


                    Case "DM"
                        Dim p = Split(cmd(1), "∙")
                        modifyrow(p(0))
                        Console.WriteLine($"Row Modified on table {p(0)}")
                        Return (returncodes.ROW_MODIFIED_SUCCESS)

                    Case "DR"
                        Dim p = Split(cmd(1), "∙")
                        deleterow(p(0))
                        Console.WriteLine($"Row Deleted on table {p(0)}")
                        Return (returncodes.ROW_DELETE_SUCCESS)

                    Case "DDT"
                        Dim p = Split(cmd(1), "∙")
                        'tablelist(tn).deletetable()
                        Return (returncodes.TABLE_DROP_SUCCESS)
                    Case "DUT"
                        Dim p = Split(cmd(1), "∙")
                        Dim tb = gettableindex(p(0))
                        tablelist(tb).updatefiles()
                        Return (returncodes.TABLE_UPDATE_SUCESS)
                    Case "DKT"

                        Dim p = Split(cmd(1), "∙")
                        Dim tb = gettableindex(p(0))
                        tablelist(tb).rekeytable()
                        Return (returncodes.TABLE_REKEY_SUCCESS)

                    Case "DSC"

                        Dim p = Split(cmd(1), "∙")
                        Dim r = Split(p(1), "·")
                        Dim tb = gettableindex(p(0))
                        tablelist(tb).updateColumn(r)
                        Return (returncodes.TABLE_UPDATE_SUCESS)

                End Select
            Catch ex As Exception
                Return (returncodes.TABLE_UPDATE_ERROR)
            End Try
        Next

    End Function

    Public Function addrow(tbl, r)
        Dim rid As Integer
        Dim idx = gettableindex(tbl)
        rid = tablelist(idx).addrow(r)
        If debugon Then Console.WriteLine($"Added New Row {rid}")
        Return rid
    End Function

    Private Function docompare(row As String(), cindex As Integer)
        If row Is Nothing Then Return False
        If llogic(cindex).type = "V" Then
            Try
                Dim v As Decimal
                Dim r = Decimal.TryParse(llogic(cindex).fieldval, v)
                If r = False Then Return False
            Catch ex As Exception
                Return (False)
            End Try
        End If
        Dim returnbool = False
        If llogic(cindex).type = "V" Then

            Select Case llogic(cindex).cflag
                Case 0

                    If Val(row(llogic(cindex).field)) = Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If Val(row(llogic(cindex).field)) <> Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If Val(row(llogic(cindex).field)) > Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If Val(row(llogic(cindex).field)) < Val(llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        Else

            Select Case llogic(cindex).cflag
                Case 0

                    If (row(llogic(cindex).field)) = (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If (row(llogic(cindex).field)) <> (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If (row(llogic(cindex).field)) > (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If (row(llogic(cindex).field)) < (llogic(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        End If

        Return returnbool
    End Function
    Public Sub deleterow(tblname As String)

        Dim idx = gettableindex(tblname)
        setllogicfieldstonum(idx)

        For Each prow As trows In tablelist(idx).tablerows.Values
            Dim rb As Boolean = True
            If prow.getdflag = 0 Then
                For tt = 0 To llogic.Count - 1
                    If docompare(prow.row, tt) = False Then
                        rb = False
                        Exit For
                    End If
                Next

                If rb = True Then
                    tablelist(idx).deleterow(prow.recordid)
                    If debugon Then Console.WriteLine($"deleting row {prow.recordid}")
                End If
            End If
        Next



    End Sub

    Private Sub setmodvalues(tbl)
        Dim idx = gettableindex(tbl)
        Dim cols As String() = tablelist(idx).getcolnames
        Try
            For tt = 0 To modfields.Count - 1
                For t = 0 To cols.Count - 1
                    If modfields(tt).columnnumber = cols(t) Then
                        modfields(tt).columnnumber = t
                        Exit For
                    End If
                Next
            Next
        Catch ex As Exception
            Debug.WriteLine(ex.Message)
        End Try


    End Sub

    Public Sub modifyrow(tbl As String)

        Dim idx = gettableindex(tbl)
        setllogicfieldstonum(idx)
        setmodvalues(tbl)


        Dim rowbool As Boolean = False
        For Each prow As trows In tablelist(idx).tablerows.Values

            If prow.getdflag = 0 Then
                For tt = 0 To llogic.Count - 1
                    If docompare(prow.row, tt) = True Then
                        rowbool = True
                    Else
                        rowbool = False
                        Exit For
                    End If
                Next

                If rowbool = True Then
                    For Each mf In modfields
                        tablelist(idx).modifyrow(prow.recordid, mf.columnnumber, mf.columnvalue)
                    Next
                    tablelist(idx).finishmodify(prow.recordid)
                End If
            End If
        Next


    End Sub





End Class

Public Class tableindex
    Public ifield As Integer
    Public dindex = New Dictionary(Of Integer, Integer)
End Class

Public Class ptable
    Public tablename As String
    Private rid As Integer
    Private columnames As String() = {""}
    'Public tablerows As New List(Of trows)
    Public tablerows As New Concurrent.ConcurrentDictionary(Of Integer, trows)


    Public Function getcolnames()
        Return columnames
    End Function
    Public Sub rekeytable()
        Dim nindex = 0
        For Each row In tablerows
            Dim nid = row.Value.row(0)
            row.Value.setrecordid(nid)
            If debugon Then Console.WriteLine($"New Key set for {nindex}")
            nindex = nid
        Next
        rid = nindex + 1
        updatefiles()
    End Sub


    Public Sub Service_Stop()
        canceltoken = True
    End Sub


    Private Sub loaddelfile()
        Dim st = Now
        If debugon Then Console.WriteLine($"Loading delete table {tablename}")
        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & ".del", System.IO.FileMode.Open, System.IO.FileAccess.Read)
        Dim binreader = New System.IO.BinaryReader(FileStream)
        Dim payload = binreader.ReadBytes(FileStream.Length)
        Dim filepointer = 0
        Dim i As Integer = 0
        Dim et = Now
        If debugon Then Console.WriteLine($"Finished Loading delete table {tablename} {(et - st).TotalSeconds}")

        st = Now
        If debugon Then Console.WriteLine($"setting delete Dbase Table {tablename}")
        While filepointer < payload.Length
            i = BitConverter.ToInt32(payload, filepointer)
            Dim b(i - 1) As Byte
            Array.Copy(payload, filepointer + 4, b, 0, i)
            Dim bs = System.Text.Encoding.UTF8.GetString(b)
            Dim ba = Split(bs, "°").ToArray
            Dim findkey = ba(1)

            For Each prow In tablerows.Values
                If findkey = prow.getrecordid Then
                    prow.setdflag(1)
                End If
            Next
            filepointer += b.Length + 4

        End While
        et = Now
        If debugon Then Console.WriteLine($"Finished Dellist {tablename} {(et - st).TotalSeconds}")

        FileStream.Flush()
        FileStream.Close()
    End Sub


    Private Sub loadmodfile()
        Dim st = Now
        If debugon Then Console.WriteLine($"Loading Alter table {tablename}")
        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & ".mod", System.IO.FileMode.Open, System.IO.FileAccess.Read)
        Dim binreader = New System.IO.BinaryReader(FileStream)
        Dim payload = binreader.ReadBytes(FileStream.Length)
        Dim filepointer = 0
        Dim i As Integer = 0
        Dim et = Now
        If debugon Then Console.WriteLine($"Finished Loading Alter table {tablename} {(et - st).TotalSeconds}")

        st = Now
        If debugon Then Console.WriteLine($"Altering Dbase Table {tablename}")
        While filepointer < payload.Length
            i = BitConverter.ToInt32(payload, filepointer)
            Dim b(i - 1) As Byte
            Array.Copy(payload, filepointer + 4, b, 0, i)
            Dim bs = System.Text.Encoding.UTF8.GetString(b)
            Dim ba = Split(bs, "°").ToArray
            Dim findkey = ba(1)

            For Each prow In tablerows.Values
                If findkey = prow.getrecordid Then
                    prow.setrow(Split(ba(2), "·"))
                End If
            Next
            filepointer += b.Length + 4

        End While
        et = Now
        If debugon Then Console.WriteLine($"Finished Modlist {tablename} {(et - st).TotalSeconds}")

        FileStream.Flush()
        FileStream.Close()

    End Sub


    Private Sub loadbinfile()
        Dim st = Now
        If debugon Then Console.WriteLine($"Loading table {tablename}")
        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & ".bin", System.IO.FileMode.Open, System.IO.FileAccess.Read)
        Dim binreader = New System.IO.BinaryReader(FileStream)
        Dim payload = binreader.ReadBytes(FileStream.Length)
        Dim filepointer = 0
        Dim i As Integer = 0
        Dim et = Now
        If debugon Then Console.WriteLine($"Finished Loading table {tablename} {(et - st).TotalSeconds}")

        st = Now
        If debugon Then Console.WriteLine($"Building Dbase Table {tablename}")
        While filepointer < payload.Length
            i = BitConverter.ToInt32(payload, filepointer)
            Dim b(i - 1) As Byte
            Array.Copy(payload, filepointer + 4, b, 0, i)
            Dim bs = System.Text.Encoding.UTF8.GetString(b)
            Dim ba = Split(bs, "°").ToArray
            Dim tr As New trows
            tr.setdflag(ba(0))
            tr.setrecordid(ba(1))
            tr.setrow(Split(ba(2), "·"))
            tablerows.TryAdd(ba(1), tr)
            filepointer += b.Length + 4

        End While
        et = Now
        If debugon Then Console.WriteLine($"Finished DBase Table {tablename} {(et - st).TotalSeconds}")

        FileStream.Flush()
        FileStream.Close()

        loadinf()



    End Sub


    Public Sub load()
        Dim needupdate = False
        loadbinfile()
        If File.Exists(Filepaths.dbasepath & tablename & ".mod") Then
            needupdate = True
            loadmodfile()
        End If
        If File.Exists(Filepaths.dbasepath & tablename & ".del") Then
            loaddelfile()
            needupdate = True
        End If

        If needupdate = True Then
            updatefiles()
        End If

    End Sub


    Public Sub createtable(tblname As String)
        tablename = tblname
        tablerows = New Concurrent.ConcurrentDictionary(Of Integer, trows)

        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tblname & ".bin", System.IO.FileMode.Create, System.IO.FileAccess.Write)
        FileStream.Flush()
        FileStream.Close()

        FileStream = New System.IO.FileStream(Filepaths.dbasepath & tblname & ".inf", System.IO.FileMode.Create, System.IO.FileAccess.Write)
        FileStream.Flush()
        FileStream.Close()
        tablelist.Add(Me)

    End Sub

    Private Function atos(cn As String()) As String
        Dim stn As String = ""
        For t = 0 To cn.Length - 1
            stn = stn & cn(t) & "·"
        Next
        stn = stn.Substring(0, stn.Length - 1)
        Return stn
    End Function

    Private Function stoa(str As String) As String()
        Dim ar = Split(str, "·")
        Return ar.ToArray
    End Function


    Private Sub updateinf()
        Dim str As String = rid.ToString
        str = str & "°" & atos(columnames)
        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & ".inf", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write)
        FileStream.Write(System.Text.Encoding.UTF8.GetBytes(str))
        FileStream.Flush()
        FileStream.Close()
    End Sub

    Public Sub updateColumn(col As String())
        Dim str As String = rid.ToString
        str = str & "°" & atos(col)
        columnames = col
        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & ".inf", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write)
        FileStream.Write(System.Text.Encoding.UTF8.GetBytes(str))
        FileStream.Flush()
        FileStream.Close()
    End Sub

    Public Sub loadinf()
        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & ".inf", System.IO.FileMode.Open, System.IO.FileAccess.Read)
        Dim binreader = New System.IO.BinaryReader(FileStream)
        Dim payload = binreader.ReadBytes(FileStream.Length)
        If payload.Length = 0 Then Exit Sub
        Dim str = System.Text.Encoding.UTF8.GetString(payload)
        Dim strs = Split(str, "°")
        If debugon Then Console.WriteLine($"Loading Info {str}")
        rid = strs(0)
        columnames = (stoa(strs(1)))
        FileStream.Flush()
        FileStream.Close()

    End Sub


    Public Sub setcolnames(cn As String())
        columnames = cn
        updateinf()
    End Sub

    Public Enum APPEND_FLAG

        ADD = 1
        MODIFY = 2
        DELETE = 3

    End Enum



    Private Sub fileappend(tr As trows, action As APPEND_FLAG)

        Dim filetype As String = ""

        Select Case action
            Case APPEND_FLAG.ADD
                filetype = "bin"
            Case APPEND_FLAG.MODIFY
                filetype = "mod"
            Case APPEND_FLAG.DELETE
                filetype = "del"
        End Select



        Dim arrays As String
        arrays = tr.getdflag & "°" & tr.getrecordid & "°"
        arrays = arrays & atos(tr.getrow)

        Dim arraysb = System.Text.Encoding.UTF8.GetBytes(arrays)

        Dim mbuffer As New List(Of Byte)

        mbuffer.AddRange(BitConverter.GetBytes(arraysb.Length))
        mbuffer.AddRange(arraysb)

        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & "." & filetype, System.IO.FileMode.Append, System.IO.FileAccess.Write)
        FileStream.Write(mbuffer.ToArray, 0, mbuffer.Count)
        FileStream.Flush()
        FileStream.Close()
        mbuffer.RemoveRange(0, 4)
        If debugon Then Console.WriteLine(System.Text.Encoding.UTF8.GetString(mbuffer.ToArray))
    End Sub
    Public Function addrow(r As String())
        Dim tr As New trows
        rid += 1
        tr.setdflag(0)
        tr.setrecordid(rid)
        Dim newr = r
        For t = 0 To r.Length - 1
            If r(t) = "🔑" Then
                newr(t) = rid
            Else
                newr(t) = r(t)
            End If
        Next
        tr.row = newr
        tablerows.TryAdd(rid, tr)
        fileappend(tr, APPEND_FLAG.ADD)
        updateinf()
        Return rid
    End Function





    Public Sub updatefiles()
        Dim FileStream = New System.IO.FileStream(Filepaths.dbasepath & tablename & ".new", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write)
        Dim mbuffer As New List(Of Byte)
        For Each tr In tablerows

            Dim arrays As String

            If tr.Value.getdflag = 0 Then

                arrays = tr.Value.getdflag & "°" & tr.Value.getrecordid & "°"
                arrays = arrays & atos(tr.Value.getrow)
                Dim arraysb = System.Text.Encoding.UTF8.GetBytes(arrays)
                mbuffer.AddRange(BitConverter.GetBytes(arraysb.Length))
                mbuffer.AddRange(arraysb)

            End If

        Next

        FileStream.Write(mbuffer.ToArray, 0, mbuffer.Count)
        FileStream.Flush()
        FileStream.Close()
        mbuffer.RemoveRange(0, 4)
        'if debugon then console.writeline(System.Text.Encoding.UTF8.GetString(mbuffer.ToArray))
        File.Delete((Filepaths.dbasepath & tablename & ".mod"))
        File.Delete((Filepaths.dbasepath & tablename & ".del"))
        File.Replace(Filepaths.dbasepath & tablename & ".new", Filepaths.dbasepath & tablename & ".bin", Filepaths.dbasepath & tablename & ".old")


    End Sub


    Public Sub deleterow(dkey As Integer)

        tablerows(dkey).setdflag(1)
        Dim tr = tablerows(dkey)
        fileappend(tr, APPEND_FLAG.DELETE)



    End Sub

    Public Sub modifyrow(mkey As Integer, colnum As Integer, colvalue As String)

        tablerows(mkey).row(colnum) = colvalue

    End Sub

    Public Sub finishmodify(mkey As Integer)
        Dim tr = tablerows(mkey)
        fileappend(tr, APPEND_FLAG.MODIFY)

    End Sub
    Public Function tablecount()
        Return tablerows.Count
    End Function

    Public Function tablerow(i As Integer)
        Return tablerows(i)
    End Function

    'Public Function updatetable()
    'Dim rval As Integer = 0
    'Return rval
    ' End Function


End Class

Public Class trows
    Implements IDisposable
    Public dflag As Integer
    Public recordid As Integer
    Public row As String()

    Public Function getdflag()
        Return dflag
    End Function

    Public Function getrecordid()
        Return recordid
    End Function

    Public Function getrow() As String()
        Return row
    End Function
    Public Sub setdflag(i As Integer)
        dflag = i
    End Sub

    Public Sub setrecordid(i As Integer)
        recordid = i
    End Sub

    Public Sub setrow(r As String())
        row = r
    End Sub

    Private Sub IDisposable_Dispose() Implements IDisposable.Dispose
        GC.Collect()
        'Throw New NotImplementedException()
    End Sub
End Class


Public Class coltoreturn
    Public colnames As New List(Of String)
    Public colindex As New List(Of Integer)
End Class

Public Class queryengine

    Private userid As String
    Private queryid As Integer
    Private quearytype As Qflags
    Public keyfield As Integer
    Public andcomparitors As New List(Of compare)
    Public orcomparitors As New List(Of compare)
    Private modifiers As New List(Of modifier)
    Private startable As ptable
    Private resulttables = New List(Of List(Of String()))
    Private canceltoken As Boolean = False
    Private qque As New Queue(Of String)
    Private tasks As New List(Of Task)
    Private cltortn As coltoreturn
    Public cols As String()

    Public Event returnrows(r As List(Of String()))




    'Split Commands alt 250
    'Split Command Args Alt 249
    'SPlit Columns Alt 248
    'SPlit String(of command) to string(of commands) Alt 247
    'Split Strings to strings(of commands) Alt 240


    Public Enum Qflags
        RSELECT = 0
        RADD = 1
        RDELETE = 2
        RMODIFY = 3
    End Enum


    Public Enum Cflags
        EQUAL = 0
        NOTEQUAL = 1
        GREATER = 2
        LESS = 3

    End Enum

    Structure compare
        Dim type As Char
        Dim field As String
        Dim cflag As Cflags
        Dim fieldval As String

        Public Sub setfield(fld)
            field = fld
        End Sub

    End Structure

    Structure modifier
        Dim fieldnumber As String
        Dim comparevalue As String
        Dim newfieldval As String
    End Structure

    Public Function atos(cn As String()) As String
        Dim stn As String = ""
        For t = 0 To cn.Length - 1
            stn = stn & cn(t) & "·"
        Next
        stn = stn.Substring(0, stn.Length - 1)
        Return stn
    End Function

    Private Function FindTableNumber(tblname) As Integer
        For t = 0 To tablelist.Count - 1
            If tablelist(t).tablename = tblname Then
                Return t
                Exit For
            End If
        Next
    End Function

    Private Function ColnameorNum(colname As String, indx As Integer)
        Dim dummyd As Decimal = 0
        Dim reslt = Decimal.TryParse(colname, dummyd)
        If reslt = True Then Return dummyd

        Dim tcols As String() = (tablelist(indx).getcolnames)
        For t = 0 To tcols.Length - 1
            If colname = tcols(t) Then
                Return t
            End If
        Next
        Return -1
    End Function


    Public Function addcoltoreturn(indx As Integer) As Integer
        Try
            For t = 0 To cols.Length - 1
                cltortn.colnames.Add(cols(t))
                cltortn.colindex.Add(ColnameorNum(cols(t), indx))
            Next
        Catch ex As Exception
            Return -1
        End Try
        Return 0
    End Function





    Public Function run_query(tbl)

        Try
            cltortn = New coltoreturn
            Dim newtable As New List(Of String())
            resulttables.clear
            Dim idx = FindTableNumber(tbl)
            Dim cname As String() = tablelist(idx).getcolnames
            Dim flds = cname.Count
            addcoltoreturn(idx)

            For t = 0 To andcomparitors.Count - 1
                Dim d = andcomparitors(t)
                d.setfield(ColnameorNum(d.field, idx))
                andcomparitors(t) = d
                If d.field = -1 Then Throw New Exception("No DatafieldName match on comparitor")
            Next

            For t = 0 To orcomparitors.Count - 1
                Dim d = orcomparitors(t)
                d.setfield(ColnameorNum(d.field, idx))
                orcomparitors(t) = d
                If d.field = -1 Then Throw New Exception("No DatafieldName match on comparitor")
            Next


            selectrows(tbl)

            If resulttables.count > 0 Then
                For tt = 0 To resulttables.count - 1
                    For zz = 0 To resulttables(tt).count - 1
                        newtable.Add(resulttables(tt)(zz))
                    Next


                Next

            End If

            Dim trimtable As New tmptable
            trimtable.colnames = cltortn.colnames
            For xx = 0 To newtable.Count - 1
                If newtable(xx).Count = flds Then
                    trimtable.rows.Add(newtable(xx))
                Else
                    If debugon Then Console.WriteLine($"Found Bad Row at {atos(newtable(xx))}")
                End If
                ' if debugon then console.writeline($"adding line {newtable(xx)(0)} {newtable(xx)(5)}")
            Next


            Return trimtable
        Catch ex As Exception
            If debugon Then Console.WriteLine($"Broke {ex.Message}")
        End Try

    End Function


    Public Function getrecordcount(tbl As String)
        For Each d In tablelist
            If d.tablename = tbl Then
                Return d.tablecount
                Exit Function
            End If
        Next
        Return 0
    End Function





    Private Function doandcompare(row As String(), cindex As Integer)
        If row Is Nothing Then Return False
        If andcomparitors(cindex).type = "V" Then
            Try
                Dim v As Decimal
                Dim r = Decimal.TryParse(andcomparitors(cindex).fieldval, v)
                If r = False Then Return False
            Catch ex As Exception
                Return (False)
            End Try
        End If
        Dim returnbool = False
        If andcomparitors(cindex).type = "V" Then

            Select Case andcomparitors(cindex).cflag
                Case 0

                    If Val(row(andcomparitors(cindex).field)) = Val(andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If Val(row(andcomparitors(cindex).field)) <> Val(andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If Val(row(andcomparitors(cindex).field)) > Val(andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If Val(row(andcomparitors(cindex).field)) < Val(andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        Else

            Select Case andcomparitors(cindex).cflag
                Case 0

                    If (row(andcomparitors(cindex).field)) = (andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If (row(andcomparitors(cindex).field)) <> (andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If (row(andcomparitors(cindex).field)) > (andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If (row(andcomparitors(cindex).field)) < (andcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        End If

        Return returnbool
    End Function

    Private Function doorcompare(row As String(), cindex As Integer)
        If row Is Nothing Then Return False
        If orcomparitors(cindex).type = "V" Then
            Try
                Dim v As Decimal
                Dim r = Decimal.TryParse(orcomparitors(cindex).fieldval, v)
                If r = False Then Return False
            Catch ex As Exception
                Return (False)
            End Try
        End If
        Dim returnbool = False
        If orcomparitors(cindex).type = "V" Then

            Select Case orcomparitors(cindex).cflag
                Case 0

                    If Val(row(orcomparitors(cindex).field)) = Val(orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If Val(row(orcomparitors(cindex).field)) <> Val(orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If Val(row(orcomparitors(cindex).field)) > Val(orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If Val(row(orcomparitors(cindex).field)) < Val(orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        Else

            Select Case orcomparitors(cindex).cflag
                Case 0

                    If (row(orcomparitors(cindex).field)) = (orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 1
                    If (row(orcomparitors(cindex).field)) <> (orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If
                Case 2
                    If (row(orcomparitors(cindex).field)) > (orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

                Case 3
                    If (row(orcomparitors(cindex).field)) < (orcomparitors(cindex).fieldval) Then
                        returnbool = True
                    End If

            End Select
        End If

        Return returnbool
    End Function


    Public Sub comparerows(tc As List(Of trows), lstart As Integer, lstop As Integer)
        Dim rt As New List(Of String())
        Dim rowbool As Boolean
        For t = lstart To lstop

            If tc(t).getdflag = 0 Then

                For tt = 0 To andcomparitors.Count - 1
                    If doandcompare(tc(t).row, tt) = True Then
                        rowbool = True
                    Else
                        rowbool = False
                        Exit For
                    End If

                Next

                If rowbool = True Then
                    'Dim newar As string = {queryid.ToString}.Union(tc.tablerows(t).row).ToArray
                    Dim newar = tc(t).row
                    rt.Add(newar)
                End If
            End If

        Next

        If rt.Count > 0 Then
            resulttables.add(rt)
        End If

    End Sub


    Public Sub Anyrows(tc As List(Of trows), lstart As Integer, lstop As Integer)
        Dim rowbool As Boolean
        Dim resulttable As New List(Of String())
        For t = lstart To lstop
            If tc(t).getdflag = 0 Then
                For tt = 0 To orcomparitors.Count - 1
                    rowbool = False
                    If doorcompare(tc(t).row, tt) = True Then
                        rowbool = True
                        For ac = 0 To andcomparitors.Count - 1
                            If doandcompare(tc(t).row, ac) = False Then
                                rowbool = False
                                Exit For
                            End If
                        Next
                        If rowbool = True Then
                            'if debugon then console.writeline($"found a match {tc.tablerows(t).row(2)} on row{t} comparitor {tt} {orcomparitors(tt).fieldval}")

                            'Dim newar As string = {queryid.ToString}.Union(tc.tablerows(t).row).ToArray
                            Dim newar = tc(t).row
                            resulttable.Add(newar)
                            'Exit For
                        End If
                    End If
                Next
            Else
                If debugon Then Console.WriteLine("Found deleted record")
            End If

        Next

        If resulttable.Count > 0 Then
            resulttables.add(resulttable)
        End If


    End Sub



    Private Function selectrows(tblname) As Task
        For Each tb In tablelist
            If tb.tablename = tblname Then
                Dim tstart = Now
                Dim DtoL = tb.tablerows.Values.ToList
                Dim tend = Now
                If debugon Then Console.WriteLine($"time to build list {(tend - tstart).TotalSeconds}")

                ' Await Task.Run(Sub()
                Dim mytasks = 0
                For t = 0 To tb.tablerows.Count - 1 Step rrange
                    Dim rowstart = t
                    Dim rowstop = t + rrange - 1

                    If rowstart > tb.tablerows.Count - 1 Then
                        rowstart = tb.tablerows.Count - 1
                    End If

                    If rowstop > tb.tablerows.Count - 1 Then
                        rowstop = tb.tablerows.Count - 1
                    End If
                    If rowstart <= rowstop Then
                        Dim tsk As New Task(Sub()
                                                If orcomparitors.Count > 0 Then
                                                    Anyrows(DtoL, rowstart, rowstop)
                                                Else
                                                    comparerows(DtoL, rowstart, rowstop)
                                                End If

                                            End Sub)
                        tsk.Start()
                        'if debugon then console.writeline($"starting new task {tsk.Id}")
                        tasks.Add(tsk)
                    End If


                Next
                Task.WaitAll(tasks.ToArray)

                If debugon Then Console.WriteLine("All Tasks Completed")
                Exit For
            End If
        Next
    End Function

    'Public Sub startengine()
    '    canceltoken = False
    '    Dim q = New Task(Sub()
    '                         If qque.Count > 0 Then
    '                             buildquery(qque.Dequeue)
    '                         End If

    '                     End Sub)
    '    q.Start()
    'End Sub

    Public Sub stopengine()
        canceltoken = True
    End Sub
End Class


Class txtlog
    Public rid
    Public myid
    Public otherid
    Public sid
    Public tbody
    Public ttimestamp
    Public inorout
    Public attachment
End Class


Class cellcontacts
    Public customerid
    Public customername
    Public cellphone

End Class
