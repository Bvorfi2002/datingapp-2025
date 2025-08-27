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

  timeLeft: number;
  private intervalId: number | null = null;

  constructor(private zone: NgZone) {
    this.timeLeft = this.duration;
  }

  ngOnInit(): void {
    this.start();
  }

  ngOnDestroy(): void {
    this.stop();
  }

  start(): void {
    this.stop();
    this.timeLeft = this.duration;

    this.zone.runOutsideAngular(() => {
      this.intervalId = window.setInterval(() => {
        this.zone.run(() => {
          this.handleTick();
        });
      }, 1000);
    });
  }

  stop(): void {
    if (this.intervalId) {
      window.clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  private handleTick(): void {
    if (this.timeLeft > 0) {
      this.timeLeft--;
    } else {
      this.timeLeft = 0;
      this.stop();
      this.finished.emit();
    }
  }

  get formattedTime(): string {
    const minutes = Math.floor(this.timeLeft / 60);
    const seconds = this.timeLeft % 60;
    return `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
  }
}
