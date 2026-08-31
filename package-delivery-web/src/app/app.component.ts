import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { TokenService } from './core/services/token.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  constructor() {
    const tokenService = inject(TokenService);
    const authService = inject(AuthService);

    // Restore the current-user signal after a page reload so layouts (e.g. the sidebar email) render correctly.
    if (tokenService.getAccessToken()) {
      authService.loadCurrentUser().subscribe({ error: () => void 0 });
    }
  }
}
