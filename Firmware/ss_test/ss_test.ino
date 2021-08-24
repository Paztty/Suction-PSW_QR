#define VS9000_brush_red   7
#define VS9000_brush_white 6
#define VS9000_brush_short 8

#define VS9500_charge_P 5
#define VS9500_charge_N 4

#define BatterySensor 2

#define BUZZER 9


// timer for sevice
#define PRESCALER                     64
#define TIMER                         25              //unit: milliseconds
#define START_COUNTER                 (65536 - (TIMER * 16 *(unsigned long)1000/PRESCALER))

#include "node.h"

const String POWER_ON     =       ":POWER_ON";
const String POWER_OFF    =       ":POWER_OFF";



int ON = 0;
int OFF = 1;
int LAST_STATE;

bool InProgress = false;
String TestResult = "TEST:RESULT:READY";

int melody[] = {
  NOTE_C4, NOTE_G3,NOTE_G3, NOTE_A3, NOTE_G3,0, NOTE_B3, NOTE_C4};

// thời gina các nốt nhạc: 4 = 1/4 nốt nhạc, 8 = 1/8nốt nhạc, ...:
int noteDurations[] = {
  4, 8, 8, 4,4,4,4,4 };


enum TEST_MODEL
{
    VS9000 = 0,
    VS9500 = 1,
};
TEST_MODEL MODEL;

unsigned char IO_REG = 0b11110001;


void setup()
{
    pinMode(BUZZER, OUTPUT);
    digitalWrite(BUZZER,LOW);

    pinMode(VS9000_brush_short, OUTPUT);
    pinMode(13, OUTPUT);

    DDRD=B00000000;// 8 pin là input 
    PORTD=B11111111;// đặt port có nội trở kéo.

    OFF = digitalRead(BatterySensor);
    ON = !digitalRead(BatterySensor);
    LAST_STATE = digitalRead(BatterySensor);

    MODEL = 0;
// timer settup
  cli();                                // tắt ngắt toàn cục

  /* Reset Timer/Counter1 */
  TCCR1A = 0;
  TCCR1B = 0;
  TIMSK1 = 0;

  /* Setup Timer/Counter1 */
  TCCR1B |= (0 << CS12) | (1 << CS11) | (1 << CS10);  // prescale = 64
  TCNT1 = 0;
  TIMSK1 = (1 << TOIE1);               // overflow interrupt enable

  sei();
// end timer settup



 Serial.begin(9600);

    PlaySong();



}

void serialEvent(){  //serialEvent
    String buffer = "";
    char start_byte = Serial.read(); 
    while(start_byte != '*')
    {
        start_byte = Serial.read();
    }
    //buffer = '*';
    buffer += Serial.readStringUntil('\r'); 
    Serial.println(buffer);
    Response(buffer);
}

void Response(String query)
{
    if(query == "Arduino?")
    {
        Serial.println("Arduino");
    }
    else if (query == "TEST:StartProgress")
    {
        InProgress = true;
    }
    else if (query == "TEST:EndProgress:PASS")
    {
        Buzzer(1,2,3);
        InProgress = false;
        digitalWrite(VS9000_brush_short, LOW);
        TestResult = "READY";
    }
    else if (query == "TEST:EndProgress:FAIL")
    {
        Buzzer(3,5,5);
        InProgress = false;
        digitalWrite(VS9000_brush_short, LOW);
        TestResult = "READY";
    }
    else if (query == "MODEL:VS9000")
    {
        MODEL = VS9000;
    }
    else if (query == "MODEL:VS9500")
    {
        MODEL = VS9500;
    }
    else if (query == "TEST:SHORT")
    {
        if(MODEL == VS9000) digitalWrite(VS9000_brush_short, HIGH);
        digitalWrite(13, !digitalRead(13));
    }
    else if (query == "TEST:RESULT?")
    {
        Serial.println(TestResult);
    }
    else if (query == "TEST:RESULT?")
    {
        Serial.println(TestResult);
    }
}

ISR (TIMER1_OVF_vect)
{
  sevice_run();
  TCNT1 = START_COUNTER;
}

void sevice_run()
{
    IO_CHECK();
    BUZZER_Service();
}

// IO check
void IO_CHECK()
{
    unsigned char read_d=PIND&B11111111;
if (MODEL == VS9500){
        if (digitalRead(BatterySensor) == ON && LAST_STATE == OFF)
        {
            delay(10);
            if (digitalRead(digitalRead(BatterySensor) == ON))
            {
                LAST_STATE = ON;
                Serial.println(POWER_ON);
            }
        }
        if (digitalRead(BatterySensor) == OFF && LAST_STATE == ON)
        {
            delay(10);
            if (digitalRead(digitalRead(BatterySensor) == OFF))
            {
                LAST_STATE = OFF;
                Serial.println(POWER_OFF);
            }
        }
}
    if(InProgress)
    {
        //Serial.println(read_d, BIN);
        switch (MODEL)
        {
        case VS9000:
            //Serial.println(read_d & B10011000, BIN);
                if ((read_d & B11100000) == B10000000)
                {
                    TestResult = "TEST:RESULT:PASS";
                }
                else
                {
                    TestResult = "TEST:RESULT:FAIL";
                }
            break;
        case VS9500:
            //Serial.println(read_d & B10011000, BIN);
            if ((read_d & B10011000) == B10001000)
            {
                TestResult = "TEST:RESULT:PASS";
            }
            else
            {
                TestResult = "TEST:RESULT:FAIL";
            }
            break;
        default:
            break;
        }
    }
}

int Buzzer_Interval = 0;
int Buzzer_Counter = 0;
int Buzzer_TimeOn = 0;
int Buzzer_Cycle = 0;
int Buzzer_Cycle_Count = 0;

void BUZZER_Service()
{
    if (Buzzer_Cycle_Count < Buzzer_Cycle)
    {
        if(Buzzer_Counter > Buzzer_Interval)
        {
            Buzzer_Counter = 0;
            Buzzer_Cycle_Count++;
        } 
        else
            {
                if (Buzzer_Counter <= Buzzer_TimeOn)
                {
                    digitalWrite(BUZZER, HIGH);
                }
                else
                {
                    digitalWrite(BUZZER, LOW);
                }
                Buzzer_Counter++;
            }
            
    }
    else
    {
        digitalWrite(BUZZER, LOW);
    }
    
}
void Buzzer(int Time_On, int Interval, int Cycle)
{
    Buzzer_Interval = Interval;
    Buzzer_Counter = 0;
    Buzzer_TimeOn = Time_On;
    Buzzer_Cycle = Cycle;
    Buzzer_Cycle_Count = 0;
}

void PlaySong()
{
      for (int thisNote = 0; thisNote < 8; thisNote++) {

    // bây giờ ta đặt một nốt nhạc là 1 giây = 1000 mili giây
    // thì ta chia cho các thành phần noteDurations thì sẽ
    // được thời gian chơi các nốt nhạc
    // ví dụ: 4 => 1000/4; 8 ==> 1000/8 
    int noteDuration = 1000/noteDurations[thisNote];
    tone(BUZZER, melody[thisNote],noteDuration);

    // để phân biệt các nốt nhạc hãy delay giữa các nốt nhạc
    // một khoảng thời gian vừa phải. Ví dụ sau đây thực hiện tốt
    // điều đó: Ta sẽ cộng 30% và thời lượng của một nốt
    int pauseBetweenNotes = noteDuration * 1.30;
    delay(pauseBetweenNotes);
    
    //Ngừng phát nhạc để sau đó chơi nhạc tiếp!
    noTone(BUZZER);
  }
}

void loop()
{

}
