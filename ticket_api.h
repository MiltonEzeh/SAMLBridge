#ifndef TICKER_API_H
#define TICKER_API_H

//#include "TicketApiErr.h"

typedef long TCK_API_INSTANCE;

extern "C" {

	// Initializes the TicketAPI
	// PARAMETERS: None
	// RETURN VALUE:  Handle of the TicketAPI instance
	// NULL handle is returned in case of any error.
	// The errorCode can be retrieved using TcktApi_getLastError
	// Possible errorcodes: TCK_API_ERR_NOTINSTALLED.

	typedef
		TCK_API_INSTANCE _TcktApi_Initialize( void );

	// Retrieves the Version of any components of the GATicket system
	// PARAMETERS
	//     TCK_API_INSTANCE -   Specifies the instance of the Ticket API.
	//                          can be NULL in case of iComponentType==GATICKET_VERSION_API
	//     int iComponentType - Specifies the component of GA Ticket system.
	//                          Possible values of this component are:-
	//                              GATICKET_VERSION_API - To retrieve the version of the GATicket API
	//                              GATICKET_VERSION_ENGINE - To retrieve the version of the GATicket engine
	// RETURN VALUE:
	//		DWORD
	//          If success,specifies the version of the requested component.
	//			If failed, 0 is returned. TcktApi_getLastError can be used to retrieve the error.

	typedef
		DWORD 
			_TcktApi_GetGAVersion(
				TCK_API_INSTANCE instance,
				int iComponentType );

	// Gets a Ticket from the Ticket engine
	// If there is no ticket available, the engine will organize one
	// PARAMETERS: 
	//		TCK_API_INSTANCE - Ticket API Instance
	//		char *pszTicketBuffer  - Pointer to a character which receives the ticket
	//		unsigned int TicketBufferSize - Size of the ticket buffer.
	//		unsigned int *piUsedTicketSize - If !=NULL This will receive the size of the ticket updated in       
    //      pszTicketBuffer in bytes.
	// RETURN VALUE:  int - If success TCK_API_SUCCESS is returned and the ticket is updated in the buffer. If failed, 
	// proper error codes are returned. When an error occurs, the parameters are not changed.
	// possible errorcodes: TCK_API_ERR_BUFFER_TOO_SMALL int TcktApi_getTicket(TCK_API_INSTANCE, char *pszTicketBuffer,             // unsigned int TicketBufferSize, unsigned int *piUsedTicketSize);

	typedef
		int 
			_TcktApi_getTicket(
				TCK_API_INSTANCE instance,
				char *pszTicketBuffer,
				unsigned int TicketBufferSize,
				unsigned int *piUsedTicketSize );

	// Gets a Ticket from the Ticket engine
	// If there is no ticket available, TCK_API_ERR_TICKET_NOT_AVAILABLE is returned
	// PARAMETERS: 
	//		TCK_API_INSTANCE - Ticket API Instance
	//		char *pszTicketBuffer  - Pointer to a character which receives the ticket
	//		unsigned int TicketBufferSize - Size of the ticket buffer.
	//		unsigned int *piUsedTicketSize - If !=NULL This will receive the size of the ticket updated in       
    //      pszTicketBuffer in bytes.
	// RETURN VALUE:  int - If success TCK_API_SUCCESS is returned and the ticket is updated in the buffer. If failed, 
	// proper error codes are returned. When an error occurs, the parameters are not changed.
	// possible errorcodes: TCK_API_ERR_BUFFER_TOO_SMALL int TcktApi_getTicket(TCK_API_INSTANCE, char *pszTicketBuffer,             // unsigned int TicketBufferSize, unsigned int *piUsedTicketSize);

	typedef
		int 
			_TcktApi_getTicketNoAuth(
				TCK_API_INSTANCE instance,
				char *pszTicketBuffer,
				unsigned int TicketBufferSize,
				unsigned int *piUsedTicketSize );

	// Gets an new Ticket from the Ticket engine
	// PARAMETERS: 
	//		TCK_API_INSTANCE - Ticket API Instance
	//		char *pszTicketBuffer  - Pointer to a character which receives the ticket
	//		unsigned int TicketBufferSize - Size of the ticket buffer.
	//		unsigned int *piUsedTicketSize - If !=NULL This will receive the size of the ticket updated in                    //          pszTicketBuffer in bytes.
	// RETURN VALUE:  int - If success TCK_API_SUCCESS is returned and the ticket is updated in the buffer. If failed, 
	// proper error codes are returned. When an error occurs, the parameters are not changed.

	typedef
		int 
			_TcktApi_getNewTicket(
				TCK_API_INSTANCE instance,
				char *pszTicketBuffer,
				unsigned int TicketBufferSize,
				unsigned int *piUsedTicketSize );

	// Destroys the existing Ticket in the Ticket engine
	// PARAMETERS:
	//		TCK_API_INSTANCE - Instance of the Ticket API
	// RETURN VALUE:  TCK_API_SUCCESS if successfull. If failed, returns an error code.
	// possible errorcodes: TCK_API_ERR_NOTINSTALLED.

	typedef
		int 
			_TcktApi_destroyTicket(
				TCK_API_INSTANCE instance );

	//Retrieves the last occured error
	//PARAMETERS 
	//	TCK_API_INSTANCE - Ticket API Instance
	//RETURN VALUE
	//	  DWORD - Value of the error code.

	typedef
		DWORD _TcktApi_getLastError( TCK_API_INSTANCE instance );

	//Retrieves an error description string to an errorcode
	//PARAMETERS:
	//	  TCK_API_INSTANCE - Ticket API Instance
	//	  DWORD dwError	- Error code for which the description need to be retrieved.
	//    char *pszDescrBuffer - Pointer to char buffer to receive the error.
	//    unsigned int DescrBufferSize - Buffer size.
	//    unsigned int *piUsedDescrSize - If !=NULL This will receive the size of the description updated to the buffer in                          //  bytes. 
	// RETURN VALUE:
	//	  TCK_API_SUCCESS, if succeeded. An error code is returned if failed.

	typedef
		int
			_TcktApi_getErrorDescription(
				TCK_API_INSTANCE instance,
				DWORD dwError,
				char *pszDescrBuffer,
				unsigned int DescrBufferSize,
				unsigned int *piUsedDescrSize );

	//Finalizes the TicketAPI Instance
	//After this the passed Ticket API instance will not be valid.
	//PARAMETERS
	//		TCK_API_INSTANCE Instance of the Ticket API.

	typedef
		void
			_TcktApi_Finalize(
				TCK_API_INSTANCE instance );

} // extern "C"



#endif //TICKER_API_H


#ifndef TICKET_API_H
#define TICKET_API_H

// Version constants
#define GATICKET_VERSION_API		1
#define GATICKET_VERSION_ENGINE		2

// Check errors
#define IsError(Err) (Err != TCK_API_SUCCESS)
#define IsGenericError(Err) (Err & TCK_ERR_GRP_GENERIC)
#define IsCardDeviceError(Err) (Err & TCK_ERR_GRP_CARD_DEVICE)
#define IsAuthError(Err) (Err & TCK_ERR_GRP_AUTHENTICATION)
#define IsConfigError(Err) (Err & TCK_ERR_GRP_CONFIG)
#define IsWindowsError(Err) ( Err & TCK_ERR_GRP_WINDOWS )

// Error Codes
#define	TCK_API_SUCCESS						0x00000000

// Error groups
#define TCK_ERR_GRP_MASK					0x80000000
#define TCK_ERR_GRP_GENERIC					0x80100000
#define TCK_ERR_GRP_CARD_DEVICE				0x80200000
#define TCK_ERR_GRP_AUTHENTICATION			0x80400000
#define TCK_ERR_GRP_CONFIG					0x80800000
#define TCK_ERR_GRP_WINDOWS					0x81000000

//GENERIC ERRORS
#define TCK_API_ERR_INTERNAL					( 0x00000001 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_NOT_INITIALIZED				( 0x00000002 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_INVALID_PARAMETER			( 0x00000003 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_BUFFER_TOO_SMALL			( 0x00000004 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_INVALID_INSTANCE_HANDLE		( 0x00000005 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_SERVER_BUSY					( 0x00000006 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_SERVER_COMM_FAILED			( 0x00000007 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_RESOURCE_ALLOCATION_FAILED	( 0x00000008 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_TICKET_NOT_AVAILABLE		( 0x00000009 |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_INTERNAL_ERROR				( 0x0000000A |TCK_ERR_GRP_GENERIC)
#define TCK_API_ERR_NOT_IMPLEMENTED				( 0x0000000b |TCK_ERR_GRP_GENERIC)

//CONFIGURATION ERRORS
#define TCK_API_ERR_VERSION_CONFLICT			( 0x00000001 |TCK_ERR_GRP_CONFIG)
#define TCK_API_ERR_NOT_INSTALLED				( 0x00000002 |TCK_ERR_GRP_CONFIG)
#define TCK_API_ERR_NO_SERVERFOUND				( 0x00000003 |TCK_ERR_GRP_CONFIG)
#define TCK_API_ERR_LOAD_RES_FAILED				( 0x00000004 |TCK_ERR_GRP_CONFIG)
#define TCK_API_ERR_AUTHMETHOD_NOT_AVAILABLE	( 0x00000005 |TCK_ERR_GRP_CONFIG)
#define TCK_API_ERR_AUTHMETHOD_NOT_POSSIBLE		( 0x00000006 |TCK_ERR_GRP_CONFIG)
#define TCK_API_ERR_RESOURCE_NOT_AVAILABLE		( 0x00000007 |TCK_ERR_GRP_CONFIG)

//CARD or DEVICE RELATED ERRORS
#define TCK_API_ERR_SMARTCARD_NOT_SUPPORTED		( 0x00000001 |TCK_ERR_GRP_CARD_DEVICE)
#define TCK_API_ERR_READER_NOT_AVAILABLE		( 0x00000002 |TCK_ERR_GRP_CARD_DEVICE)
#define TCK_API_ERR_SMARTCARD_NOT_VALID			( 0x00000003 |TCK_ERR_GRP_CARD_DEVICE)
#define TCK_API_ERR_PIN_BLOCKED					( 0x00000004 |TCK_ERR_GRP_CARD_DEVICE)
#define TCK_API_ERR_CARD_TRANSACTION_FAILED		( 0x00000005 |TCK_ERR_GRP_CARD_DEVICE)
#define TCK_API_ERR_NO_CERT_FOUND				( 0x00000006 |TCK_ERR_GRP_CARD_DEVICE)
#define TCK_API_ERR_NO_KEY_FOUND				( 0x00000007 |TCK_ERR_GRP_CARD_DEVICE)

//AUTHENTICATION ERRORS
#define TCK_API_ERR_USER_ABORT					( 0x00000001 |TCK_ERR_GRP_AUTHENTICATION)
#define TCK_API_ERR_AUTHENTICATION_FAILED		( 0x00000002 |TCK_ERR_GRP_AUTHENTICATION)
#define TCK_API_ERR_AUTHENTICATION_NOT_POSSIBLE	( 0x00000003 |TCK_ERR_GRP_AUTHENTICATION)
#define TCK_API_ERR_USER_NOT_VALID				( 0x00000004 |TCK_ERR_GRP_AUTHENTICATION)
#define TCK_API_ERR_AUTHSERVER_NOT_AVAILABLE	( 0x00000005 |TCK_ERR_GRP_AUTHENTICATION)
#define TCK_API_ERR_AUTHDATA_SYNTAX_ERROR		( 0x00000006 |TCK_ERR_GRP_AUTHENTICATION)
#define TCK_API_ERR_AUTH_NOT_VALID				( 0x00000007 |TCK_ERR_GRP_AUTHENTICATION )
#define TCK_API_ERR_AUTHMETHOD_NOT_SUPPORTED	( 0x00000008 |TCK_ERR_GRP_AUTHENTICATION )

#endif //TICKET_API_H


