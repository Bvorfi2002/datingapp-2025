import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { LoginCreds, RegisterCreds, User } from '../../types/user';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LikesService } from './likes-service';
import { PresenceService } from './presence-service';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private http = inject(HttpClient);
  private likesService = inject(LikesService);
  private presenceService = inject(PresenceService);
  currentUser = signal<User | null>(null);

  baseUrl = environment.apiUrl;

  register(creds: RegisterCreds){
    return this.http.post<User>(this.baseUrl + 'account/register', creds,
       {withCredentials:true}).pipe(
      tap(user => {
        if (user) {
          // this.setCurrentUser(user);
          this.starTokenRefreshInterval();
        }
      })
    )

  }

  confirmEmail(email: string, code: string) {
  return this.http.post(this.baseUrl + 'account/confirm-email',
    { email, code },
    { withCredentials: true }
  );
}

confirmPhone(email: string, code: string) {
  return this.http.post<User>(this.baseUrl + 'account/confirm-phone',
    { email, code },
    { withCredentials: true }
  );
}

resendConfirmationCode(email: string) {
  return this.http.post(this.baseUrl + 'account/resend-confirmation-code', 
    JSON.stringify(email), 
    {
      headers: { 'Content-Type': 'application/json' },
      withCredentials: true
    }
  );
}

  login(creds: LoginCreds) {
    return this.http.post<User>(this.baseUrl + 'account/login', creds,
       {withCredentials:true}).pipe(
      tap(user => {
        if (user) {
          this.setCurrentUser(user);
          this.starTokenRefreshInterval();
        }
      })
    )
  }

  refreshToken(){
    return this.http.post<User>(this.baseUrl + 'account/refresh-token', {}, {withCredentials:true})
  }

  starTokenRefreshInterval(){
    setInterval(() => {
     this.http.post<User>(this.baseUrl + 'account/refresh-token', {}, {withCredentials:true}).subscribe({
      next: user => {
        this.setCurrentUser(user);
      },
      error: () => {
        this.logout();
      }
     })
    },14 * 25 *  60 * 60 * 1000)//14 days
  }
  
  setCurrentUser(user: User) {
    user.roles = this.getRolesFromToken(user);
    this.currentUser.set(user);
    this.likesService.getLikeIds();
    if(this.presenceService.hubConnection?.state !== HubConnectionState.Connected){
      this.presenceService.createHubConnection(user)
    }
  }

  logout() {
    this.http.post(this.baseUrl + 'account/logout', {} ,{withCredentials:true}).subscribe({
      next: () => {
      localStorage.removeItem('filters');
      this.likesService.clearLikeIds();
      this.currentUser.set(null);
      this.presenceService.stopHubConnection();
      }
    })
  }

  private getRolesFromToken(user: User): string[] {
    const payload = user.token?.split('.')[1];
    const decoded = atob(payload!);
    const jsonPayLoad = JSON.parse(decoded);

    return Array.isArray(jsonPayLoad.role) ? jsonPayLoad.role : [jsonPayLoad.role]

  }
}
  

