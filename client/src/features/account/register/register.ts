import { Component, inject, model, output, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { RegisterCreds } from '../../../types/user';
import { AccountService } from '../../../core/services/account-service';
import { TextInput } from '../../../shared/text-input/text-input';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NgOtpInputModule } from 'ng-otp-input';
import { ToastService } from '../../../core/services/toast-service';
import { CountdownComponent } from '../../../shared/countdown/countdown';


@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, TextInput, FormsModule, NgOtpInputModule, CountdownComponent],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private accountService = inject(AccountService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);
  cancelRegister = output<boolean>();
  protected creds = {} as RegisterCreds;
  protected credentialsForm: FormGroup;
  protected profileForm: FormGroup;
  protected currentStep = signal(1);
  protected validationErrors = signal<string[]>([]);
  protected emailOtp = '';
  protected smsOtp = '';
  protected registeredEmail = '';
  protected canResend = signal(true);

  constructor() {
    this.credentialsForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      displayName: ['', Validators.required],
      phoneNumber: ['', Validators.required],
      password: [
        '',
        [Validators.required, Validators.minLength(4), Validators.maxLength(8)],
      ],
      confirmPassword: [
        '',
        [Validators.required, this.matchValues('password')],
      ],
    });

    this.profileForm = this.fb.group({
      gender: ['male', Validators.required],
      dateOfBirth: ['', Validators.required],
      city: ['', Validators.required],
      country: ['', Validators.required],
    });

    this.credentialsForm.controls['password'].valueChanges.subscribe(() => {
      this.credentialsForm.controls['confirmPassword'].updateValueAndValidity();
    });
  }

  matchValues(matchTo: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const parent = control.parent;
      if (!parent) return null;

      const matchValue = parent.get(matchTo)?.value;
      return control.value === matchValue ? null : { passwordMismatch: true };
    };
  }

  nextStep() {
    if (this.credentialsForm.valid) {
      this.currentStep.update((prevStep) => prevStep + 1);
    }
  }

  prevStep() {
    this.currentStep.update((prevStep) => prevStep - 1);
  }

  getMaxDate() {
    const today = new Date();
    today.setFullYear(today.getFullYear() - 18);
    return today.toISOString().split('T')[0];
  }

  // register() {
  //   if (this.profileForm.valid && this.credentialsForm.valid) {
  //     const formData = {
  //       ...this.credentialsForm.value,
  //       ...this.profileForm.value,
  //     };
  //     this.registeredEmail = formData.email;

  //     this.accountService.register(formData).subscribe({
  //       next: () => {
  //         this.currentStep.set(3); // Go to Email OTP step
  //       },
  //       error: (error) => {
  //         console.error('Registration failed', error);
  //         this.validationErrors.set(error);
  //       },
  //     });
  //   }
  // }
  startCountdown() {
    this.canResend.set(false);
  }

  onCountdownFinished() {
    this.canResend.set(true);
  }

  submitProfile() {
    if (this.profileForm.valid && this.credentialsForm.valid) {
      // --- START: FIX ---
      // Manually create the payload object to exclude confirmPassword
      const creds = this.credentialsForm.value;
      const profile = this.profileForm.value;

      const registerPayload = {
        email: creds.email,
        displayName: creds.displayName,
        phoneNumber: creds.phoneNumber,
        password: creds.password,
        gender: profile.gender,
        dateOfBirth: profile.dateOfBirth,
        city: profile.city,
        country: profile.country,
      };
      // --- END: FIX ---

      this.registeredEmail = registerPayload.email;

      this.accountService.register(registerPayload).subscribe({
        next: () => {
          this.currentStep.set(3);
          this.startCountdown();
        },
        error: (err) => {
          this.validationErrors.set(err.error.errors || ['Registration failed. Please try again.']);
          this.toast.warning('Please check the form for errors.');
        }
      });
    }
  }

  confirmEmailOtp() {
  this.accountService.confirmEmail(this.registeredEmail, this.emailOtp).subscribe({
    next: () => {
      this.currentStep.set(4); 
      this.startCountdown(); 
    },
    error: error => {
      const errorMsg = error.error || 'Invalid email OTP';
      this.toast.error(errorMsg);
      this.validationErrors.set([errorMsg]);
    }
  });
}

confirmSmsOtp() {
  this.accountService.confirmPhone(this.registeredEmail, this.smsOtp).subscribe({
    next: user => {
      this.accountService.setCurrentUser(user);
      this.accountService.starTokenRefreshInterval();
      this.router.navigateByUrl('/members');
    },
    error: error => {
      const errorMsg = error.error || 'Invalid SMS OTP';
      this.toast.error(errorMsg);
      this.validationErrors.set([errorMsg]);
    }
  });
}

resendCode() {
  if (!this.canResend) return;

  this.accountService.resendConfirmationCode(this.registeredEmail).subscribe({
    next: () => {
      this.toast.sucess("A new confirmation code has been sent");
      this.startCountdown(); 
    },
    error: () => {
      this.toast.error("Failed to resend confirmation code");
    }
  });
}



  cancel() {
    this.cancelRegister.emit(false);
  }

  otpConfig = {
  length: 6,
  allowNumbersOnly: true,
  inputClass: "w-12 h-12 border-2 border-gray-400 rounded-md text-center text-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-300 transition"
};

onOtpChange(otp: string) {
  console.log("Entered OTP: ", otp);
}

onEmailOtpChange(otp: string) {
  this.emailOtp = otp;
}

onSmsOtpChange(otp: string) {
  this.smsOtp = otp;
}
}
