#include <SimpleKalmanFilter.h>
#include <Servo.h>
#include <Wire.h>

// Định nghĩa chân kết nối
#define IN1 3
#define IN2 4
#define ENA 5
#define servo 9
const int trigPin = 6;
const int echoPin = 7;
// #define TRIG_PIN 6
// #define ECHO_PIN 7
// #define LED_PIN 12

// Biến toàn cục
long duration;
int distance;
//VL53L0X sensor; // Tạm thời không dùng cảm biến VL53L0X
SimpleKalmanFilter simpleKalmanFilter(2, 2, 0.1);
// Serial output refresh time
const long SERIAL_REFRESH_TIME = 100;
long refresh_time;
Servo myservo;

int motorSpeed = 0; // Tốc độ của băng tải, mặc định

void setup() 
{
    pinMode(12, OUTPUT);
    digitalWrite(12, 0);
    Serial.begin(9600);
    myservo.attach(servo);
    
    // Cấu hình chân đầu vào/ra
    
    pinMode(IN1, OUTPUT);
    pinMode(IN2, OUTPUT);
    pinMode(ENA, OUTPUT);
    
    pinMode(trigPin, OUTPUT);
    pinMode(echoPin, INPUT);
    //Serial.begin(9600);
    /*
    // Khởi động sensor VL53L0X (Tạm thời vô hiệu hóa)
    Wire.begin();
    sensor.setTimeout(100);
    
    if (!sensor.init()) {
        Serial.println("Lỗi: Không tìm thấy cảm biến VL53L0X!");
        while (1);
    }
    
    sensor.startContinuous();
    */
}

void loop() 
{
    // --- Đọc khoảng cách từ cảm biến HC-SR04 ---
    /*digitalWrite(trigPin, LOW);
    delayMicroseconds(2);
    digitalWrite(trigPin, HIGH);
    delayMicroseconds(10);
    digitalWrite(trigPin, LOW);
    
    duration = pulseIn(echoPin, HIGH);
    distance = duration * 0.01715; // Công thức chính xác hơn
    
    Serial.print("Distance: ");
    Serial.println(distance);
*/
    /*
    // --- Đọc khoảng cách từ VL53L0X (Tạm thời vô hiệu hóa) ---
    float real_value = sensor.readRangeContinuousMillimeters(); // Đọc giá trị từ VL53L0X
    float estimated_value = simpleKalmanFilter.updateEstimate(real_value);
    Serial.print("VL53L0X Distance: ");
    Serial.println(estimated_value);
    */

    // --- Điều khiển băng tải dựa trên khoảng cách đo được ---
    /*if (distance < 120 && distance > 50) 
    {
        delay(2400);
        digitalWrite(IN1, 0); // Tắt băng tải
        digitalWrite(IN2, 0); // Tắt băng tải
        delay(2000);
        Serial.println("Capimage");
        delay(5000);
        digitalWrite(IN1, 1); // Bật băng tải
        digitalWrite(IN2, 0); // Bật băng tải
    }*/

    // --- Xử lý lệnh từ Serial ---
    if (Serial.available() > 0) 
    {
        String x = Serial.readStringUntil('#');
        x.trim();
        String command = x.substring(1,6);
        //Serial.print(command);
        if (command == "speed") {
            String pwm = x.substring(7);
            analogWrite(ENA,pwm.toInt());
            Serial.print(pwm);
        }
        //int speed = Serial.parseInt();
        
        if (x == "*den=1") digitalWrite(12, 1); // Bật đèn
        if (x == "*den=0") digitalWrite(12, 0); // Tắt đèn

        if (x.startsWith("*bangtai=1")) {

          int speed = x.toInt();
          if (speed >=0 && speed <= 255){
            analogWrite(ENA,speed);
            digitalWrite(IN1, 1);
            digitalWrite(IN2, 0);
            Serial.print("Running conveyor at speed: "+ speed);
          } else {
            Serial.println("Invalid speed value. Please provide a value between 0 and 255.");
          }
        }
        else if (x.startsWith("*bangtai=0")) {
            digitalWrite(IN1, 0); // Tắt băng tải
            digitalWrite(IN2, 0);
            Serial.println("Conveyor stopped."); 
        }
        
        if (x == "*mach=good") myservo.write(0);
        if (x == "*mach=bad") {
            myservo.write(90);
            delay(3000);
            myservo.write(0);
        }
    }
}
