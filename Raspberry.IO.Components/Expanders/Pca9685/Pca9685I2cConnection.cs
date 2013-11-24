﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Raspberry.IO.InterIntegratedCircuit;


namespace Raspberry.IO.Components.Expanders.Pca9685
{
    /// <summary>
    /// Driver for PCA9685
    /// 16-channel, 12-bit PWM Fm+ I2C-bus LED controller

    /// Ported from
    /// https://github.com/adafruit/Adafruit-Raspberry-Pi-Python-Code/blob/master/Adafruit_PWM_Servo_Driver/Adafruit_PWM_Servo_Driver.py
    /// </summary>
    public class PCA9685I2cConnection
    {
        private readonly I2cDeviceConnection connection;

        private enum Register
        {
           SUBADR1 = 0x02,
           SUBADR2            = 0x03,
           SUBADR3            = 0x04,
           MODE1              = 0x00,
           PRESCALE           = 0xFE,
           LED0_ON_L          = 0x06,
           LED0_ON_H          = 0x07,
           LED0_OFF_L         = 0x08,
           LED0_OFF_H         = 0x09,
           ALLLED_ON_L        = 0xFA,
           ALLLED_ON_H        = 0xFB,
           ALLLED_OFF_L       = 0xFC,
           ALLLED_OFF_H       = 0xFD,

        }

        private void Log(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public PCA9685I2cConnection(I2cDeviceConnection connection)
        {
            this.connection = connection;

            Log("Reseting PCA9685");
            WriteRegister(Register.MODE1, 0x00);
        }

        /// <summary>
        /// Datasheet: 7.3.5 PWM frequency PRE_SCALE
        /// </summary>
        public void SetPWMUpdateRate(int frequencyHz)
        {
            var preScale = 25000000.0m; // 25MHz
            preScale /= 4096m;// 12-bit
            preScale /= frequencyHz;

            preScale -= 1.0m;

            Log("Setting PWM frequency to {0} Hz", frequencyHz);
            Log("Estimated pre-scale: {0}", preScale);

            var prescale = Math.Floor(preScale + 0.5m);

            Log("Final pre-scale: {0}", prescale);

            var oldmode = ReadRegister(Register.MODE1);
            var newmode = (byte) ((oldmode & 0x7F) | 0x10);      // sleep


            WriteRegister(Register.MODE1, newmode);         // go to sleep

            WriteRegister(Register.PRESCALE, (byte) Math.Floor(prescale));
            WriteRegister(Register.MODE1, oldmode);
            
            Timers.Timer.Sleep(5);
            
            WriteRegister(Register.MODE1, oldmode| 0x80);
        }

        /// <summary>
        /// Sets a single PWM channel
        /// </summary>
        public void SetPWM(int channel, int on, int off)
        {
            WriteRegister(Register.LED0_ON_L + 4*channel, on & 0xFF);
            WriteRegister(Register.LED0_ON_H + 4*channel, on >> 8);
            WriteRegister(Register.LED0_OFF_L + 4*channel, off & 0xFF);
            WriteRegister(Register.LED0_OFF_H + 4*channel, off >> 8);
        }

        private void WriteRegister(Register register, byte data)
        {
            Log("{0}=>{1}", register, data);
            connection.Write(new[] { (byte)register , data });
        }

        private void WriteRegister(Register register, int data)
        {
           WriteRegister(register, (byte) data);
        }

        private byte ReadRegister(Register register)
        {
            connection.Write((byte)register);
            var value = connection.ReadByte();
            return value;
        }
    }
}