/*
 * Project  : NHS Connecting for Health
 * Subsystem: SAML Bridge
 * Component: Identity Agent Ticket API ActiveX Wrapper
 *
 * IDE      : Microsoft Visual Studio 2005
 * Solution : IATicket
 * Project  : IATicket
 * File     : UTicketException.cpp
 *
 * Updated  : Nick James
 * Company  : Fujitsu Services
 * Date     : 16 May 2006
 *
 * This is a simple wrapper class around CAtlException. It extends its base class by adding
 * a string member to contain an exception message, plus an associated accessor method. It is
 * used to manage error conditions within the CTicket class.
 */
#include "StdAfx.h"
#include "UTicketException.h"

UTicketException::UTicketException(HRESULT hr, CComBSTR& bstrMessage)
	: m_bstrMessage(bstrMessage),
	CAtlException(hr)
{}

UTicketException::~UTicketException(void) {}

CComBSTR& UTicketException::GetMessage(void)
{
	return m_bstrMessage;
}
