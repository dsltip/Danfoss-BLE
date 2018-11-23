/**************************************************************************************************
  Filename:       uartManager.c
  Description:    handles command send over the serial interface
**************************************************************************************************/

/*********************************************************************
 * INCLUDES
 */

#include "OSAL.h"
#include "OSAL_PwrMgr.h"

#include "simpleBLECentral.h"

#include "hal_led.h"
#include "hal_uart.h"
#include "hal_flash.h"
#include "uartManager.h"

/*********************************************************************
 * MACROS
 */

/*********************************************************************
 * CONSTANTS
 */
#define BAUD_RATE                  HAL_UART_BR_9600
//#define BAUD_RATE                  HAL_UART_BR_115200
#define POLL_INTERVAL               50
#define READ_TRIES_MAX              10


// responses
#define RESPONSE_DEVICE_FOUND          0x02  //used for sending results of device discovery

// events
#define UART_START_DEVICE_EVT 0x01
#define UART_PERIODIC_EVT     0x02

/*********************************************************************
 * TYPEDEFS
 */

/*********************************************************************
 * GLOBAL VARIABLES
 */
uint8 uartManagerTaskID;
uint8 rxlen;
uint8 stage=0;
/*********************************************************************
 * LOCAL FUNCTIONS
 */
static void uartManager_ProcessOSALMsg( osal_event_hdr_t *pMsg );
static void HalUARTCback (uint8 port, uint8 event);
static void uartSendMsg(uint8 num );
//static void execute_command();

/*********************************************************************
 * LOCAL VARIABLES
 */
static halUARTCfg_t config =
{
  .configured = TRUE,
  .baudRate = BAUD_RATE,
  .flowControl = FALSE,
  .idleTimeout = 100,
  .rx = { .maxBufSize = 255 },
  .tx = { .maxBufSize = 255 },
  .intEnable = TRUE,
  .callBackFunc = HalUARTCback
};

//pahses

static uint8 opcode = 0;


/*********************************************************************
 * PUBLIC FUNCTIONS
 */

/*********************************************************************
 * @fn      uartManager_Init
 *
 * @brief   TODO
 *
 * @param   task_id - the ID assigned by OSAL.  This ID should be
 *                    used to send messages and set timers.
 *
 * @return  none
 */
void uartManager_Init( uint8 task_id )
{
  uartManagerTaskID = task_id;
  osal_set_event(uartManagerTaskID, UART_START_DEVICE_EVT );
}


static void uartSendMsg(uint8 num ){
            myDataMsg_t* msg = (myDataMsg_t*) osal_msg_allocate(sizeof(myDataMsg_t));
            msg->event = CB_MSG_MY_DATA;
            msg->length = 2;
            osal_msg_send(simpleBLETaskId, (uint8*) msg);
            uartchr = num;
};
/*********************************************************************
 * @fn      uartManager_ProcessEvent
 *
 * @brief   This function is called to process all events for the task. Events
 *          include timers, messages and any other user defined events.
 *
 * @param   task_id  - The OSAL assigned task ID.
 * @param   events - events to process.  This is a bit map and can
 *                   contain more than one event.
 *
 * @return  events not processed
 */
uint16 uartManager_ProcessEvent( uint8 task_id, uint16 events )
{
  VOID task_id; // OSAL required parameter that isn't used in this function
  //HalLedSet( HAL_LED_1, HAL_LED_MODE_ON );
  uint8 buflen;
  uint8 rxbuf[20];
  uint8 j,maxlen;
  if ( events & SYS_EVENT_MSG )
  {
    uint8 *pMsg;
    if ( (pMsg = osal_msg_receive(uartManagerTaskID )) != NULL )
    {
      uartManager_ProcessOSALMsg( (osal_event_hdr_t *)pMsg );
      VOID osal_msg_deallocate( pMsg ); //Release the OSAL message
    }
    return (events ^ SYS_EVENT_MSG);  //return unprocessed events
  }
  
  if ( events & UART_START_DEVICE_EVT )
  {
    //HalLedSet( HAL_LED_1, HAL_LED_MODE_ON );
    HalUARTOpen(HAL_UART_PORT_1, &config);
    osal_start_reload_timer(uartManagerTaskID, UART_PERIODIC_EVT, POLL_INTERVAL);
    return ( events ^ UART_START_DEVICE_EVT );
  }
  
  if ( events & UART_PERIODIC_EVT)
  { 
      buflen = Hal_UART_RxBufLen(HAL_UART_PORT_1);
      //rxcnt = rxcnt + buflen;
    if( buflen > 0)
    { 
        //HalUARTRead(HAL_UART_PORT_1, &opcode, 1);
        HalUARTRead(HAL_UART_PORT_1, rxbuf, buflen );
        for(j=0;j<buflen;j++){
         opcode =  rxbuf[j];
        if (stage==0){
          if(opcode == 6){
            stage = 6;
            rxlen=0;
          } else if(opcode == 2){
            stage = 2;
            rxlen=0;
          } else {
            uartSendMsg(opcode);
          }
        } else {
          uartdata[rxlen] = opcode;
          rxlen++;
          if (stage==6)maxlen=8; else maxlen=6;
          if(rxlen==maxlen){
            uartSendMsg(stage);
            stage=0;
          };
        };
    };//for
    }
    return ( events ^ UART_PERIODIC_EVT );
  }
  
  // Discard unknown events
  return 0;
}

/*********************************************************************
 * @fn      uartManager_ProcessOSALMsg
 * @brief   Process an incoming task message.
 * @param   pMsg - message to process
 * @return  none
 */
static void uartManager_ProcessOSALMsg( osal_event_hdr_t *pMsg )
{
  /*switch ( pMsg->event )
  {
    case CB_MSG_DEVICE_FOUND:
    {
      deviceFoundMsg_t* msg = (deviceFoundMsg_t*) pMsg;
      msg->event = RESPONSE_DEVICE_FOUND;
      HalUARTWrite(HAL_UART_PORT_1, (uint8*) msg, msg->length + sizeof(uint8) + sizeof(uint8) );
      break;
    }
  }*/
}

/***************************
 * @fn       execute_command()
 * @breif    executes the command send over serial
 */


/**
 * @brief      UART event callback
 */
static void HalUARTCback(uint8 port, uint8 event)
{
  switch(event)
  {
    case HAL_UART_RX_FULL:
    case HAL_UART_RX_ABOUT_FULL:
    case HAL_UART_RX_TIMEOUT:
    case HAL_UART_TX_FULL:
    case HAL_UART_TX_EMPTY:
    break;
  }
}

/** 
#define FLASH_PAGE_SIZE             2048
//This could be added in future versions
static void command_flash()
{
  while(Hal_UART_RxBufLen(HAL_UART_PORT_1) == 0); //wait for first data
  HalLedSet(HAL_LED_1, HAL_LED_MODE_ON);
  HalFlashErase(0);
  uint8 current_page = 0;
  uint32 current_byte = 0;
  uint8 buf[4];
  
  //start writing to flash
  while(TRUE)
  {
    while(Hal_UART_RxBufLen(HAL_UART_PORT_1) < 4) ;  //wait for at least 4 bytes
    HalLedSet(HAL_LED_1, HAL_LED_MODE_TOGGLE);
    if(current_byte == FLASH_PAGE_SIZE)
    {
      current_page++;
      HalFlashErase(current_page);
      current_byte=0;
    }
    HalUARTRead(HAL_UART_PORT_1, buf, 4);
    HalFlashWrite(current_byte / 4, buf, 1); 
    current_byte += 4;
  }
}
**/

/*********************************************************************
*********************************************************************/
