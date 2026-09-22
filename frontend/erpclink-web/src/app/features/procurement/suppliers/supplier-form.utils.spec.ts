import { FormControl, FormGroup } from '@angular/forms';
import { buildSupplierFormValidators, isSupplierFormReady } from './supplier-form.utils';

describe('supplier form utils', () => {
  it('marks incomplete forms as not ready', () => {
    expect(isSupplierFormReady({ name: '   ' })).toBeFalse();
  });

  it('accepts minimal valid name', () => {
    expect(isSupplierFormReady({ name: 'Acme Supplies' })).toBeTrue();
  });

  it('applies required validator on name', () => {
    const v = buildSupplierFormValidators();
    const form = new FormGroup({
      name: new FormControl('', { nonNullable: true, validators: v.name }),
      email: new FormControl('', { nonNullable: true, validators: v.email })
    });
    expect(form.valid).toBeFalse();
    form.controls.name.setValue('Supplier');
    expect(form.valid).toBeTrue();
  });
});
