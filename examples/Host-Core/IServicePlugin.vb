Public Interface IServicePlugin

    Sub addservice(id)
    Sub addhostservice(id)
    Sub addclientservice(id)

    Sub dropservice(id)
    Sub drophostservice(id)
    Sub dropclientservice(id)

    Sub DoProcess()

End Interface
