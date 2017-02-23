/*
 * Project  : NHS Connecting for Health
 * Subsystem: SAML Bridge
 * Component: Identity Agent Ticket API ActiveX Wrapper
 *
 * IDE      : Microsoft Visual Studio 2005
 * Solution : IATicket
 * Project  : IATicket
 * File     : UTicketException.h
 *
 * Updated  : Nick James
 * Company  : Fujitsu Services
 * Date     : 16 May 2006
 *
 * Header file for the UTicketException class.
 */
#pragma once

class UTicketException :
	public CAtlException
{
public:
	UTicketException(HRESULT hr, CComBSTR& bstrMessage);
public:
	~UTicketException(void);
	CComBSTR& GetMessage(void);

private:
	CComBSTR m_bstrMessage;
};
