#include <SimpleKalmanFilter.h>
#include <Servo.h>
#include <Wire.h>

//------------------- KHAI BÁO CÁC CHÂN KẾT NỐI -------------------//
#define sensorPin 11 
#define IN1 3
#define IN2 4
#define ENA 5
#define servo1Pin 9
#define servo2Pin 10

SimpleKalmanFilter simpleKalmanFilter(2, 2, 0.1);
const long SERIAL_REFRESH_TIME = 100;
long refresh_time;
Servo servo1;
Servo servo2;

int motorSpeed = 0;

void setup() {
    Serial.begin(9600);

    pinMode(sensorPin, INPUT_PULLUP);
    pinMode(12, OUTPUT);
    digitalWrite(12, 0);
    pinMode(IN1, OUTPUT);
    pinMode(IN2, OUTPUT);
    pinMode(ENA, OUTPUT);

    servo1.attach(servo1Pin);
    servo2.attach(servo2Pin);
    servo1.write(0);
    servo2.write(0);
}

void loop() {
    //----Đọc giá trị từ cảm biến----//
    int sensorVal = digitalRead(sensorPin);
    // Serial.println(sensorVal);
    if (sensorVal == 0) {
      delay(2200);
      digitalWrite(IN1, 0);
      digitalWrite(IN2, 0);
      delay(8000);
      Serial.println("*capture#");
      delay(3000);
      digitalWrite(IN1, 1);  // Bật băng tải
      digitalWrite(IN2, 0);  // Bật băng tải
    }

    if (Serial.available() > 0) {
        String x = Serial.readStringUntil('#');

        if (x.startsWith("*bangtai=1")) {
          digitalWrite(IN1, 1); 
          digitalWrite(IN2, 0);
           Serial.println("run");
        }

        String command = x.substring(0,7);
        if (command == "*speed="){
          String pwm = x.substring(7);
          analogWrite(ENA,pwm.toInt());
          Serial.println("Conveyor run with speed"+ pwm);
        }
        else if (x.startsWith("*bangtai=0")) {
          digitalWrite(IN1, 0); // Tắt băng tải
          digitalWrite(IN2, 0);

        if (x == "*den=1") digitalWrite(12, 1); // Bật đèn
        if (x == "*den=0") digitalWrite(12, 0); // Tắt đèn

          Serial.println("Conveyor stopped."); 
        }

        if (x == "*mach=good") {
          servo1.write(0);
          servo2.write(0);
        }
        else if (x == "*mach=miss") {
          delay(3000);
          digitalWrite(IN1, 0);
          digitalWrite(IN2, 0);
          servo1.write(80);
          delay(3000);
          servo1.write(0);
          digitalWrite(IN1, 1);
          digitalWrite(IN2, 0); 
        }
        else if (x == "*mach=bad") {
          delay(6000);
          digitalWrite(IN1, 0);
          digitalWrite(IN2, 0);
          servo2.write(80);
          delay(3000);
          servo2.write(0);
          digitalWrite(IN1, 1);
          digitalWrite(IN2, 0);
        }
    }
}
