import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, NgZone } from '@angular/core';

@Component({
  selector: 'app-countdown',
  standalone: true,
  imports: [],
  templateUrl: './countdown.html',
  styleUrl: './countdown.css'
})
export class CountdownComponent implements OnInit, OnDestroy {
  @Input() duration: number = 180; // default 3 minutes
  @Output() finished = new EventEmitter<void>();

  timeLeft!: number;
  private intervalId: any;

  // Inject NgZone to manually control change detection
  constructor(private zone: NgZone) {}

  ngOnInit(): void {
    this.start();
  }

  start(): void {
    // Stop any existing timer before starting a new one
    this.stop();
    this.timeLeft = this.duration;

    // Run the interval outside of Angular's zone to prevent unnecessary change detection cycles on every tick
    this.zone.runOutsideAngular(() => {
      this.intervalId = setInterval(() => {
        // Run the update logic back inside Angular's zone
        this.zone.run(() => {
          if (this.timeLeft > 0) {
            this.timeLeft--;
          } else {
            this.stop();
            this.finished.emit();
          }
        });
      }, 1000);
    });
  }

  stop(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  ngOnDestroy(): void {
    this.stop();
  }

  // This formatting logic remains the same as it was already correct
  get formattedTime(): string {
    if (this.timeLeft === undefined || this.timeLeft < 0) {
        return '0:00';
    }
    const minutes = Math.floor(this.timeLeft / 60);
    const seconds = this.timeLeft % 60;
    return `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
  }
}
