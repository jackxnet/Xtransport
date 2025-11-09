Module Globals
    Public tablelist As New List(Of ptable)
    'Public resultque As New Queue(Of List(Of String()))
    Public mytt = -1
    Public rrange = 10000
    Public canceltoken = False
    Public DbaseQue As New Concurrent.ConcurrentQueue(Of Byte())

    Public Enum returncodes
        TABLE_CREATE_SUCCESS = 100
        TABLE_CREATE_ERROR = 101
        TABLE_DROP_SUCCESS = 110
        TABLE_DROP_ERROR = 111
        ROW_ADD_SUCESS = 200
        ROW_ADD_ERROR = 201
        ROW_DELETE_SUCCESS = 210
        ROW_DELETE_ERROR = 211
        ROW_MODIFIED_SUCCESS = 220
        ROW_MODIFIED_ERROR = 221
        TABLE_REKEY_SUCCESS = 300
        TABLE_REKEY_ERROR = 301
        TABLE_UPDATE_SUCESS = 400
        TABLE_UPDATE_ERROR = 401


    End Enum

End Module
